using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using OCRFunctionApp.Model;

namespace OCRFunctionApp
{
    public static class DisplayTextMSOCR
    {
        /// <summary>
        /// Display the text recognized by MSOCR for all pages in Tiff files.
        /// </summary>
        /// <param name="documents">CosmosDB documents containing the results of the text recognition.</param>
        /// <param name="log">Trace logger.</param>
        [FunctionName("DisplayTextMSOCR")]
        public static void Run(
            [CosmosDBTrigger(databaseName: "Tiff2Text", collectionName: "MSOCRResults", ConnectionStringSetting = "CosmosDBConnection", CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> documents,
            TraceWriter log)
        {
            if (documents != null && documents.Count > 0)
            {
                foreach (var document in documents)
                {
                    var name = document.GetPropertyValue<string>("name");
                    log.Info($"[DisplayTextMSOCR({name})] Showing recognized text...");

                    var pages = document.GetPropertyValue<List<MSOCRPage>>("pages");
                    var output = new StringBuilder();
                    int pageIndex = 1;
                    foreach (var page in pages)
                    {
                        output.AppendLine($"[DisplayTextMSOCR({name} - Page {pageIndex} of {pages.Count})]");
                        foreach (var region in page.Regions)
                        {
                            foreach (var line in region.Lines)
                            {
                                output.Append("    ");
                                foreach (var word in line.Words)
                                {
                                    output.Append($"{word.Text} ");
                                }
                                output.AppendLine();
                            }
                        }
                        pageIndex++;
                    }

                    log.Info(output.ToString());
                    log.Info($"[DisplayTextMSOCR({name})] Finished showing recognized text!");
                }
            }
        }
    }
}
