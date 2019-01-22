using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using OCRFunctionApp.Model;

namespace OCRFunctionApp
{
    public static class DisplayTextOneOCR
    {
        /// <summary>
        /// Display the text recognized by OneOCR for all pages in Tiff files.
        /// </summary>
        /// <param name="documents">CosmosDB documents containing the results of the text recognition.</param>
        /// <param name="log">Trace logger.</param>
        [FunctionName("DisplayTextOneOCR")]
        public static void Run(
            [CosmosDBTrigger(databaseName: "Tiff2Text", collectionName: "OneOCRResults", ConnectionStringSetting = "CosmosDBConnection", CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> documents,
            TraceWriter log)
        {
            if (documents != null && documents.Count > 0)
            {
                foreach (var document in documents)
                {
                    var name = document.GetPropertyValue<string>("name");
                    log.Info($"[DisplayTextOneOCR({name})] Showing recognized text...");

                    var pages = document.GetPropertyValue<List<OneOCRPage>>("pages");
                    var output = new StringBuilder();
                    int pageIndex = 1;
                    foreach (var page in pages)
                    {
                        output.AppendLine($"[DisplayTextOneOCR({name} - Page {pageIndex} of {pages.Count})]");
                        foreach (var line in page.Lines)
                        {
                            output.AppendLine($"    {line.Text}");
                        }
                        pageIndex++;
                    }

                    log.Info(output.ToString());
                    log.Info($"[DisplayTextOneOCR({name})] Finished showing recognized text!");
                }
            }
        }
    }
}
