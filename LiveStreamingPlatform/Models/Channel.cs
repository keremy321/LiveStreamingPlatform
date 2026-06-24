using System.ComponentModel.DataAnnotations;

namespace LiveStreamingPlatform.Models
{
    public class Channel
    {
        public int Id { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        public string StreamKey { get; set; } = Guid.NewGuid().ToString("N");

        public bool IsLive { get; set; } = false;

        public string? CurrentTitle { get; set; }
        
        public string? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
