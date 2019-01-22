using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using OCRFunctionApp.Helpers;
using OCRFunctionApp.Model;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace OCRFunctionApp
{
    /// <summary>
    /// Information used to build this sample:
    /// - <a href="https://docs.microsoft.com/en-us/azure/cognitive-services/computer-vision/quickstarts-sdk/csharp-hand-text-sdk">Quickstart: Extract text using the Computer Vision SDK and C#</a>
    /// - <a href="https://github.com/Azure/azure-sdk-for-net/tree/5161d5e6b410cbe8022479ff69e4a93e7ad43e3e/src/SDKs/CognitiveServices/dataPlane/Vision/ComputerVision">azure-sdk-for-net/src/SDKs/CognitiveServices/dataPlane/Vision/ComputerVision/</a>
    /// - <a href="https://westus.dev.cognitive.microsoft.com/docs/services/5adf991815e1060e6355ad44/operations/587f2c6a154055056008f200">Computer Vision API - v2.0 - Recognize Text</a>
    ///   "
    ///   Supported image formats: JPEG, PNG and BMP. Image file size must be less than 4MB. Image dimensions must be at least 40 x 40, at most 3200 x 3200.
    ///   "
    /// - <a href="https://westus.dev.cognitive.microsoft.com/docs/services/5adf991815e1060e6355ad44/operations/587f2cf1154055056008f201">Computer Vision API - v2.0 - Get Recognize Text Operation Result</a>
    ///   "
    ///   Result fields include lines, words, bounding box and text: 
    ///     + Lines: An array of objects, where each object represents a line of recognized text.
    ///     + Words: An array of objects, where each object represents a recognized word.
    ///     + BoundingBox: Bounding box of a recognized region, line, or word, depending on the parent object. The eight integers represent the four points(x-coordinate, y-coordinate) of the detected rectangle from the left-top corner and clockwise.
    ///     + Text: String value of a recognized word/line.
    ///   "
    /// - <a href="https://stackoverflow.com/questions/11668945/convert-tiff-to-jpg-format">convert tiff to jpg format</a>
    /// </summary>
    public static class Tiff2TextOneOCR
    {
        private const int maxWidth = 3200;
        private const int maxHeight = 3200;
        private const int numberOfCharsInOperationId = 36;
        private const int maxRetries = 10;

        /// <summary>
        /// Recognize text in Tiff file with OneOCR.
        /// </summary>
        /// <param name="tiff">Stream to the blob containing the Tiff file.</param>
        /// <param name="name">Name of the Tiff file.</param>
        /// <param name="result">Collector of results to store in CosmosDB.</param>
        /// <param name="log">Trace logger.</param>
        /// <returns></returns>
        [FunctionName("Tiff2TextOneOCR")]
        public static async Task Run(
            [BlobTrigger("tiff2text-oneocr/{name}", Connection = "InputStorage")] Stream tiff, 
            string name, 
            [DocumentDB(databaseName: "Tiff2Text", collectionName: "OneOCRResults", CreateIfNotExists = true, ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<OneOCRResult> result, 
            TraceWriter log)
        {
            var docName = $"{name}";
            log.Info($"[Tiff2TextOneOCR({docName})] Processing {tiff.Length} bytes...");

            var docPages = new List<OneOCRPage>();

            using (var image = Image.FromStream(tiff))
            {
                var frameDimensions = new FrameDimension(image.FrameDimensionsList[0]);
                int frameCount = image.GetFrameCount(frameDimensions);
                for (int frame = 0; frame < frameCount; frame++)
                {
                    // Select a page and ensure its image size is supported by the text recognition service
                    var pageName = $"{name} - Page {frame + 1} of {frameCount}";
                    image.SelectActiveFrame(frameDimensions, frame);
                    var resizedImage = image.Fit(maxWidth, maxHeight);

                    using (var ms = new MemoryStream())
                    {
                        // Convert page image to a formart supported by the text recognition service (e.g. JPEG)
                        resizedImage.Save(ms, ImageFormat.Jpeg);
                        ms.Position = 0;

                        // Recognize text in page
                        var pageResult = await RecognizeTextInStreamAsync(ms, log, $"[Tiff2TextOneOCR({pageName})]");
                        ValidateTextOperationResult(pageResult, pageName);
                        docPages.Add(new OneOCRPage()
                        {
                            Width = resizedImage.Width,
                            Height = resizedImage.Height,
                            OriginalWidth = image.Width,
                            OriginalHeight = image.Height,
                            Lines = pageResult.RecognitionResult.Lines
                        });
                    }
                }
            }

            // Add recognized text in all pages to CosmosDB
            await result.AddAsync(new OneOCRResult { Name = docName, Pages = docPages });

            log.Info($"[Tiff2TextOneOCR({docName})] Finished processing!");
        }

        // Recognize the text in a Stream with OneOCR
        private static async Task<TextOperationResult> RecognizeTextInStreamAsync(Stream stream, TraceWriter log, string logHeader)
        {
            var subscriptionKey = ConfigurationManager.AppSettings["OneOCRSubscriptionKey"];
            using (var computerVision = new ComputerVisionClient(
                   new ApiKeyServiceClientCredentials(subscriptionKey),
                   new System.Net.Http.DelegatingHandler[] { })
            {
                // E.g. Docker Container running in localhost: http://localhost:5000, Service running in West US region: https://westus.api.cognitive.microsoft.com
                Endpoint = ConfigurationManager.AppSettings["OneOCREndpoint"] 
            })
            {
                log.Info($"{logHeader} Starting text recognition with OneOCR...");

                // Start the async process to recognize the text
                var textHeaders = await computerVision.RecognizeTextInStreamAsync(stream, TextRecognitionMode.Printed);

                // Retrieve the URI where the recognized text will be stored from the Operation-Location header
                var operationId = textHeaders.OperationLocation.Substring(textHeaders.OperationLocation.Length - numberOfCharsInOperationId);

                // Retrieve the recognized text
                return await GetTextOperationResultAsync(computerVision, operationId, log, logHeader);
            }
        }

        // Wait for the text recognition to complete
        private static async Task<TextOperationResult> GetTextOperationResultAsync(ComputerVisionClient computerVision, string operationId, TraceWriter log, string logHeader)
        {
            int attempt = 1;
            TextOperationResult result;
            do
            {
                log.Info($"{logHeader} Waiting {attempt} second(s)...");
                await Task.Delay(attempt * 1000);

                result = await computerVision.GetTextOperationResultAsync(operationId);
                log.Info($"{logHeader} Text recognition server status: {result.Status}");
            } while (
                (result.Status == TextOperationStatusCodes.Running || result.Status == TextOperationStatusCodes.NotStarted)
                && attempt++ < maxRetries);

            return result;
        }

        // Fail if we didn't get a valid result from the recognition service
        private static void ValidateTextOperationResult(this TextOperationResult result, string name)
        {
            switch (result.Status)
            {
                case TextOperationStatusCodes.Failed:
                    throw new FunctionInvocationException($"Text recognition server wasn't able to recognize text in '{name}'");
                case TextOperationStatusCodes.NotStarted:
                    throw new FunctionInvocationException($"Text recognition server wasn't able to start recognizing text in '{name}'");
                case TextOperationStatusCodes.Running:
                    throw new FunctionTimeoutException($"Text recognition server didn't respond on a reasonable time frame when recognizing text in '{name}'");
            }
        }
    }
}
