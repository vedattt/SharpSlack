using System.Collections.Generic;

namespace SharpSlack.JsonObjects
{
    public class UserListResponse
    {
        public bool Ok { get; set; }
        public IList<SlackUser> Members { get; set; }
    }
}
