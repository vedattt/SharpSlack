using System.Collections.Generic;

namespace SharpSlack.JsonObjects
{
    public class ConversationListResponse
    {
        public bool Ok { get; set; }
        public IList<SlackChannel> Channels { get; set; }
    }
}

