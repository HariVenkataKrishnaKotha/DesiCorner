using Newtonsoft.Json;

namespace DesiCorner.Services.CartAPI.Services;

public class ProductService : IProductService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProductService> _logger;
    private readonly string _productApiUrl;

    public ProductService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ProductService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _productApiUrl = configuration["ServiceUrls:ProductAPI"] ?? "https://localhost:7101";
    }

    public async Task<ProductDetails?> GetProductAsync(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"{_productApiUrl}/api/products/{productId}", ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Product {ProductId} not found", productId);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var apiResponse = JsonConvert.DeserializeObject<ProductApiResponse>(json);

            if (apiResponse?.IsSuccess != true || apiResponse.Result == null)
            {
                return null;
            }

            return new ProductDetails
            {
                Id = apiResponse.Result.Id,
                Name = apiResponse.Result.Name,
                Price = apiResponse.Result.Price,
                ImageUrl = apiResponse.Result.ImageUrl,
                IsAvailable = apiResponse.Result.IsAvailable
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", productId);
            return null;
        }
    }

    private class ProductApiResponse
    {
        public bool IsSuccess { get; set; }
        public ProductDto? Result { get; set; }
    }

    private class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }
}