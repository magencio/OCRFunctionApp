using Newtonsoft.Json;
using System.Collections.Generic;

namespace OCRFunctionApp.Model
{
    /// <summary>
    /// Text recognition results for a document. MSOCR version.
    /// </summary>
    public class MSOCRResult
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "MSOCR";

        /// <summary>
        /// Name of the document
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Text recognition results for all pages in the document
        /// </summary>
        [JsonProperty(PropertyName = "pages")]
        public IList<MSOCRPage> Pages { get; set; }
    }
}
