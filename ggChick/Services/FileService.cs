namespace ggChick.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private readonly string[] _allowedMimeTypes = { "image/jpeg", "image/png", "image/webp" };
        private const long MaxFileSizeInBytes = 5 * 1024 * 1024; // 5 MB

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> UploadImageAsync(IFormFile? file, string folder = "products")
        {
            if (file == null || file.Length == 0)
                return null;

            // 1. Validate file size
            if (file.Length > MaxFileSizeInBytes)
                throw new InvalidOperationException("File size exceeds 5MB limit.");

            // 2. Validate extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
                throw new InvalidOperationException("Invalid file format. Only JPG, JPEG, PNG, and WEBP files are allowed.");

            // 3. Validate MIME type
            if (!_allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new InvalidOperationException("Invalid MIME type for image.");

            // 4. Safe folder destination in wwwroot/uploads/{folder}
            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", folder);
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            // 5. Unique, collision-free safe filename
            var uniqueFileName = $"{Guid.NewGuid():N}_{DateTime.UtcNow.Ticks}{extension}";
            var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for web and database storage
            return $"/uploads/{folder}/{uniqueFileName}";
        }

        public async Task<List<string>> UploadMultipleImagesAsync(IEnumerable<IFormFile>? files, string folder = "products")
        {
            var uploadedPaths = new List<string>();
            if (files == null) return uploadedPaths;

            foreach (var file in files)
            {
                if (file != null && file.Length > 0)
                {
                    var path = await UploadImageAsync(file, folder);
                    if (!string.IsNullOrEmpty(path))
                    {
                        uploadedPaths.Add(path);
                    }
                }
            }

            return uploadedPaths;
        }

        public bool DeleteFile(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath)) return false;

            // Trim leading slash
            var trimmed = relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, trimmed);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
