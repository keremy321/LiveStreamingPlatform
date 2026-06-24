using System.ComponentModel.DataAnnotations;

namespace LiveStreamingPlatform.Models.ViewModels
{
    public class ChannelFormViewModel
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        public string? CurrentTitle { get; set; }

        public string? Category { get; set; }
    }
}
