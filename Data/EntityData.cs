using Newtonsoft.Json;
using UnityEngine;

namespace SimpleLabels.Data
{
    /// <summary>
    /// DTO for a single label: GUID, text, colors, sizes, and optional GameObject reference.
    /// </summary>
    /// <remarks>
    /// Used by LabelTracker (in-memory), LabelDataManager (persistence), and LabelNetworkManager (sync).
    /// <see cref="GameObject"/> is <see cref="JsonIgnore"/> and not serialized; it is bound at runtime
    /// when entities are loaded or created. Colors are stored as HTML hex (e.g. <c>#FF0000</c>).
    /// </remarks>
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
