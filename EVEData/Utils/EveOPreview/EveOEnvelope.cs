using System.Text.Json;

namespace EVEData.Utils.EveOPreview
{
    public class EveOEnvelope
    {
        public EveOEnvelope(Guid RequestId, string Type, JsonElement Payload)
        {
            this.RequestId = RequestId;
            this.Type = Type;
            this.Payload = Payload;
        }

        public Guid RequestId { get; set; }
        public string Type { get; set; }
        public JsonElement Payload { get; set; }
    }
}
