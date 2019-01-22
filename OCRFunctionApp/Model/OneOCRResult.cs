using Newtonsoft.Json;
using System.Collections.Generic;

namespace OCRFunctionApp.Model
{
    /// <summary>
    /// Text recognition results for a document. OneOCR version.
    /// </summary>
    public class OneOCRResult
    {
        [JsonProperty(PropertyName = "type")]
        public string Type = "OneOCR";

        /// <summary>
        /// Name of the document.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Text recognition results for all pages in the document.
        /// </summary>
        [JsonProperty(PropertyName = "pages")]
        public IList<OneOCRPage> Pages { get; set; }
    }
}
