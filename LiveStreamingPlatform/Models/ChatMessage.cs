using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;

namespace LiveStreamingPlatform.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ChannelId { get; set; }

        public Channel channel { get; set; } = null;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
