using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Products;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesiCorner.Services.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        IImageStorageService imageStorageService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var products = await _productService.GetAllProductsAsync(ct);
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all products");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving products"
            });
        }
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(Guid categoryId, CancellationToken ct)
    {
        try
        {
            var products = await _productService.GetProductsByCategoryAsync(categoryId, ct);
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving products"
            });
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id, ct);
            if (product == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = product
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving product"
            });
        }
    }

    /// <summary>
    /// Create new product with optional image (Admin only)
    /// Accepts multipart/form-data with product JSON and optional image file
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
    [FromForm] CreateProductDto dto,
    IFormFile? image,
    CancellationToken ct)
    {
        try
        {
            // Upload image if provided
            if (image != null && image.Length > 0)
            {
                dto.ImageUrl = await _imageStorageService.UploadImageAsync(image, "products", ct);
            }

            var product = await _productService.CreateProductAsync(dto, ct);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ResponseDto
            {
                IsSuccess = true,
                Message = "Product created successfully",
                Result = product
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error creating product"
            });
        }
    }

    /// <summary>
    /// Update product with optional image (Admin only)
    /// Accepts multipart/form-data with product JSON and optional image file
    /// If image is provided, old image is deleted and new one is uploaded
    /// If image is not provided, existing image is kept
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
    Guid id,
    [FromForm] UpdateProductDto dto,
    IFormFile? image,
    CancellationToken ct)
    {
        if (id != dto.Id)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = "ID mismatch"
            });
        }

        try
        {
            // Get existing product to handle image
            var existingProduct = await _productService.GetProductByIdAsync(id, ct);
            if (existingProduct == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            // Upload new image if provided
            if (image != null && image.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                {
                    await _imageStorageService.DeleteImageAsync(existingProduct.ImageUrl, ct);
                }

                // Upload new image
                dto.ImageUrl = await _imageStorageService.UploadImageAsync(image, "products", ct);
            }
            else
            {
                // Keep existing image URL if no new image provided
                dto.ImageUrl = existingProduct.ImageUrl;
            }

            var product = await _productService.UpdateProductAsync(dto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Product updated successfully",
                Result = product
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating product"
            });
        }
    }

    /// <summary>
    /// Update ONLY product image (Admin only)
    /// Convenience endpoint when you only want to change the image
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateImage(Guid id, IFormFile image, CancellationToken ct)
    {
        try
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "No image provided"
                });
            }

            var existingProduct = await _productService.GetProductByIdAsync(id, ct);
            if (existingProduct == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            // Delete old image
            if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
            {
                await _imageStorageService.DeleteImageAsync(existingProduct.ImageUrl, ct);
            }

            // Upload new image
            var imageUrl = await _imageStorageService.UploadImageAsync(image, "products", ct);

            // Update product with new image URL
            var updateDto = new UpdateProductDto
            {
                Id = id,
                Name = existingProduct.Name,
                Description = existingProduct.Description,
                Price = existingProduct.Price,
                CategoryId = existingProduct.CategoryId,
                ImageUrl = imageUrl,
                IsAvailable = existingProduct.IsAvailable,
                IsVegetarian = existingProduct.IsVegetarian,
                IsVegan = existingProduct.IsVegan,
                IsSpicy = existingProduct.IsSpicy,
                SpiceLevel = existingProduct.SpiceLevel,
                Allergens = existingProduct.Allergens,
                PreparationTime = existingProduct.PreparationTime
            };

            var updatedProduct = await _productService.UpdateProductAsync(updateDto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Image updated successfully",
                Result = updatedProduct
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image for product {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating image"
            });
        }
    }

    /// <summary>
    /// Delete product image (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/image")]
    public async Task<IActionResult> DeleteImage(Guid id, CancellationToken ct)
    {
        try
        {
            var existingProduct = await _productService.GetProductByIdAsync(id, ct);
            if (existingProduct == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            if (string.IsNullOrEmpty(existingProduct.ImageUrl))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product has no image"
                });
            }

            // Delete image
            await _imageStorageService.DeleteImageAsync(existingProduct.ImageUrl, ct);

            // Update product to remove image URL
            var updateDto = new UpdateProductDto
            {
                Id = id,
                Name = existingProduct.Name,
                Description = existingProduct.Description,
                Price = existingProduct.Price,
                CategoryId = existingProduct.CategoryId,
                ImageUrl = null,
                IsAvailable = existingProduct.IsAvailable,
                IsVegetarian = existingProduct.IsVegetarian,
                IsVegan = existingProduct.IsVegan,
                IsSpicy = existingProduct.IsSpicy,
                SpiceLevel = existingProduct.SpiceLevel,
                Allergens = existingProduct.Allergens,
                PreparationTime = existingProduct.PreparationTime
            };

            var updatedProduct = await _productService.UpdateProductAsync(updateDto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Image deleted successfully",
                Result = updatedProduct
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image for product {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error deleting image"
            });
        }
    }

    /// <summary>
    /// Delete product (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            // Get product to delete its image
            var product = await _productService.GetProductByIdAsync(id, ct);

            // Delete image if exists
            if (product?.ImageUrl != null)
            {
                await _imageStorageService.DeleteImageAsync(product.ImageUrl, ct);
            }

            var result = await _productService.DeleteProductAsync(id, ct);
            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Product not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Product deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error deleting product"
            });
        }
    }
}