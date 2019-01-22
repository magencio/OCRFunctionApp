using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OCRFunctionApp.Model
{
    /// <summary>
    /// Text recognition results for a page in a document. MSOCR version.
    /// </summary>
    public class MSOCRPage
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
        /// BCP-47 language code of the text.
        /// </summary>
        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

        /// <summary>
        /// Angle, in degrees, of the detected text.
        /// </summary>
        [JsonProperty(PropertyName = "textAngle")]
        public double TextAngle { get; set; }

        /// <summary>
        /// Orientation of the text.
        /// </summary>
        [JsonProperty(PropertyName = "orientation")]
        public string Orientation { get; set; }

        /// <summary>
        /// Recognized regions of text in the page.
        /// </summary>
        [JsonProperty(PropertyName = "regions")]
        public IList<OcrRegion> Regions { get; set; }
    }
}
