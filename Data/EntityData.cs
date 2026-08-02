using Newtonsoft.Json;
using UnityEngine;

namespace SimpleLabels.Data
{
    public class EntityData
    {
        public EntityData()
        {
        }

        public EntityData(string guid, GameObject gameObject, string labelText, string labelColor,
            int labelSize, int fontSize, string fontColor)
        {
            Guid = guid;
            GameObject = gameObject;
            LabelText = labelText;
            LabelSize = labelSize;
            LabelColor = labelColor;
            FontSize = fontSize;
            FontColor = fontColor;
        }

        public EntityData(string guid, string labelText, string labelColor,
            int labelSize, int fontSize, string fontColor)
            : this(guid, null, labelText, labelColor, labelSize, fontSize, fontColor)
        {
        }

        public string Guid { get; set; }

        [JsonIgnore] 
        public GameObject GameObject { get; set; }

        public string LabelText { get; set; }
        public int LabelSize { get; set; }
        public string LabelColor { get; set; }
        public int FontSize { get; set; }
        public string FontColor { get; set; }
    }
}
