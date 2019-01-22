using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OCRFunctionApp.Model
{
    /// <summary>
    /// Text recognition results for a page in a document. OneOCR version.
    /// </summary>
    public class OneOCRPage
    {
        /// <summary>
        /// Width before scaling the image of the page to 
        /// the required size supported by the text recognizer service.
        /// </summary>
        [JsonProperty(PropertyName = "originalWidth")]
        public int OriginalWidth;

        /// <summary>
        /// Height before scaling .
        /// </summary>
        [JsonProperty(PropertyName = "originalHeight")]
        public int OriginalHeight;

        /// <summary>
        /// Width after scaling.
        /// </summary>
        [JsonProperty(PropertyName = "width")]
        public int Width;

        /// <summary>
        /// Height after scaling.
        /// </summary>
        [JsonProperty(PropertyName = "height")]
        public int Height;

        /// <summary>
        /// Recognized lines of text in the page.
        /// </summary>
        [JsonProperty(PropertyName = "lines")]
        public IList<Line> Lines;
    }
}
