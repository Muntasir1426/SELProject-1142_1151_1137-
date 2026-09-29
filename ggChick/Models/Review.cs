using System.ComponentModel.DataAnnotations;

namespace ggChick.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Review comment is required")]
        [MaxLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
