using System.ComponentModel.DataAnnotations;

namespace ggChick.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsMain { get; set; } = false;

        // Navigation property
        public virtual Product? Product { get; set; }
    }
}
