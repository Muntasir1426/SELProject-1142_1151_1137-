using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using ggChick.Models;

namespace ggChick.ViewModels.Products
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();

        // Filter / Search / Sort parameters
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinRating { get; set; }
        public bool InStockOnly { get; set; }
        public string Sort { get; set; } = "newest";

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        public ReviewInputModel NewReview { get; set; } = new ReviewInputModel();
        public CommentInputModel NewComment { get; set; } = new CommentInputModel();
        public bool UserHasReviewed { get; set; }
        public Review? UserExistingReview { get; set; }
    }

    public class ReviewInputModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Please select a rating between 1 and 5 stars.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 5;

        [Required(ErrorMessage = "Please write a review comment.")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "Review must be between 5 and 1000 characters.")]
        public string Comment { get; set; } = string.Empty;
    }

    public class CommentInputModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Comment cannot be empty.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Comment must be between 2 and 2000 characters.")]
        public string Content { get; set; } = string.Empty;
    }

    public class ProductCreateEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required.")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters.")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 1000000.00, ErrorMessage = "Price must be greater than 0.")]
        [Display(Name = "Price (৳)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required.")]
        [Range(0, 100000, ErrorMessage = "Stock cannot be negative.")]
        [Display(Name = "Stock Quantity")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Category is required.")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Main Image URL (or upload below)")]
        public string? MainImageUrl { get; set; }

        [Display(Name = "Upload Main Image")]
        public IFormFile? MainImageFile { get; set; }

        [Display(Name = "Upload Additional Images")]
        public List<IFormFile>? AdditionalImageFiles { get; set; }

        public SelectList? CategoriesList { get; set; }
        public List<ProductImage> ExistingImages { get; set; } = new List<ProductImage>();
    }
}
