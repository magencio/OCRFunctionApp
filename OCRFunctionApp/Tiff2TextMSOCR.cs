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
    /// - <a href="https://westus.dev.cognitive.microsoft.com/docs/services/5adf991815e1060e6355ad44/operations/56f91f2e778daf14a499e1fc">Computer Vision API - v2.0 - OCR</a>
    ///   "
    ///   Supported image formats: JPEG, PNG, GIF, BMP. Image file size must be less than 4MB. Image dimensions must be between 40 x 40 and 3200 x 3200 pixels, and the image cannot be larger than 10 megapixels.
    ///   The OCR results in the hierarchy of region/line/word. The results include text, bounding box for regions, lines and words.
    ///   "
    /// - <a href="https://stackoverflow.com/questions/11668945/convert-tiff-to-jpg-format">convert tiff to jpg format</a>
    /// </summary>
    public static class Tiff2TextMSOCR
    {
        private const int maxWidth = 3200;
        private const int maxHeight = 3200;

        /// <summary>
        /// Recognize text in Tiff file with MSOCR.
        /// </summary>
        /// <param name="tiff">Stream to the blob containing the Tiff file.</param>
        /// <param name="name">Name of the Tiff file.</param>
        /// <param name="result">Collector of results to store in CosmosDB.</param>
        /// <param name="log">Trace logger.</param>
        /// <returns></returns>
        [FunctionName("Tiff2TextMSOCR")]
        public static async Task Run(
            [BlobTrigger("tiff2text-msocr/{name}", Connection = "InputStorage")] Stream tiff, 
            string name, 
            [DocumentDB(databaseName: "Tiff2Text", collectionName: "MSOCRResults", CreateIfNotExists = true, ConnectionStringSetting = "CosmosDBConnection")] IAsyncCollector<MSOCRResult> result, 
            TraceWriter log)
        {
            var docName = $"{name}";
            log.Info($"[Tiff2TextMSOCR({docName})] Processing {tiff.Length} bytes...");

            var docPages = new List<MSOCRPage>();

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
                        var pageResult = await RecognizeTextInStreamAsync(ms, log, $"[Tiff2TextMSOCR({pageName})]");
                        docPages.Add(new MSOCRPage()
                        {
                            Width = resizedImage.Width,
                            Height = resizedImage.Height,
                            OriginalWidth = image.Width,
                            OriginalHeight = image.Height,
                            Language = pageResult.Language,
                            Orientation = pageResult.Orientation,
                            TextAngle = pageResult.TextAngle,
                            Regions = pageResult.Regions
                        });
                    }
                }
            }

            // Add recognized text in all pages to CosmosDB
            await result.AddAsync(new MSOCRResult { Name = docName, Pages = docPages });

            log.Info($"[Tiff2TextMSOCR({docName})] Finished processing!");
        }

        // Recognize the text in a Stream with MSOCR
        private static async Task<OcrResult> RecognizeTextInStreamAsync(Stream stream, TraceWriter log, string logHeader)
        {
            var subscriptionKey = ConfigurationManager.AppSettings["MSOCRSubscriptionKey"];
            using (var computerVision = new ComputerVisionClient(
                   new ApiKeyServiceClientCredentials(subscriptionKey),
                   new System.Net.Http.DelegatingHandler[] { })
            {
                // E.g. Service running in West US region: https://westus.api.cognitive.microsoft.com. No Docker container support.
                Endpoint = ConfigurationManager.AppSettings["MSOCREndpoint"]
            })
            {
                log.Info($"{logHeader} Recognizing text with MSOCR...");

                return await computerVision.RecognizePrintedTextInStreamAsync(true, stream, OcrLanguages.En);
            }
        }
    }
}
