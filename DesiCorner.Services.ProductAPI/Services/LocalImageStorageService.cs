namespace DesiCorner.Services.ProductAPI.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<LocalImageStorageService> _logger;
    private readonly string _baseUrl;

    public LocalImageStorageService(
        IWebHostEnvironment environment,
        ILogger<LocalImageStorageService> logger,
        IConfiguration configuration)
    {
        _environment = environment;
        _logger = logger;
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:7101";
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size must be less than 5MB");

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var imageUrl = $"{_baseUrl}/uploads/{folder}/{fileName}";
            _logger.LogInformation("✅ Image uploaded: {ImageUrl}", imageUrl);

            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading image");
            throw;
        }
    }

    public Task<bool> DeleteImageAsync(string imageUrl, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(imageUrl))
                return Task.FromResult(true);

            var uri = new Uri(imageUrl);
            var relativePath = uri.LocalPath.TrimStart('/');
            var filePath = Path.Combine(_environment.WebRootPath, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("✅ Image deleted: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting image: {ImageUrl}", imageUrl);
            return Task.FromResult(false);
        }
    }

    public string GetImageUrl(string fileName, string folder)
    {
        return $"{_baseUrl}/uploads/{folder}/{fileName}";
    }
}