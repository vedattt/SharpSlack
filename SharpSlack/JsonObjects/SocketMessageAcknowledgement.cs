using Newtonsoft.Json;

namespace SharpSlack.JsonObjects
{
    public class SocketMessageAcknowledgement
    {
        [JsonProperty("envelope_id")]
        public string EnvelopeId { get; set; }
    }
}

