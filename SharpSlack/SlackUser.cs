namespace SharpSlack
{
    public class SlackUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string RealName { get; set; }
        public SlackUserProfile Profile { get; set; }
    }

    public class SlackUserProfile
    {
        public string Email { get; set; }
    }
}
