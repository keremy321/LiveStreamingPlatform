namespace LiveStreamingPlatform.Models
{
    public class Follow
    {
        public int Id { get; set; }

        public string FollowerId { get; set; } = string.Empty;

        public int ChannelId { get; set; }

        public Channel channel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
