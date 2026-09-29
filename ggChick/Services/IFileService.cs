using Microsoft.AspNetCore.Http;

namespace ggChick.Services
{
    public interface IFileService
    {
        Task<string?> UploadImageAsync(IFormFile? file, string folder = "products");
        Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile>? files, string folder = "products");
        bool DeleteFile(string relativeFilePath);
    }
}
