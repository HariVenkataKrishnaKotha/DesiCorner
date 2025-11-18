namespace DesiCorner.Services.ProductAPI.Services;

public interface IImageStorageService
{
    Task<string> UploadImageAsync(IFormFile file, string folder, CancellationToken ct = default);
    Task<bool> DeleteImageAsync(string imageUrl, CancellationToken ct = default);
    string GetImageUrl(string fileName, string folder);
}