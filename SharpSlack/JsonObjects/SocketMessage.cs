using Newtonsoft.Json;

namespace SharpSlack.JsonObjects
{
    public class SocketMessage
    {
        [JsonProperty("envelope_id")]
        public string EnvelopeId { get; set; }

    }
}
