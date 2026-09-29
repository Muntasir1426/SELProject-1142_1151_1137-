using System.ComponentModel.DataAnnotations;

namespace ggChick.Models
{
    public class Comment
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Comment text is required")]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
