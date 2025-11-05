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
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
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
    /// Create new product (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        try
        {
            var product = await _productService.CreateProductAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ResponseDto
            {
                IsSuccess = true,
                Message = "Product created successfully",
                Result = product
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
    /// Update product (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
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
            var product = await _productService.UpdateProductAsync(dto, ct);
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
                Message = "Product updated successfully",
                Result = product
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
    /// Delete product (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
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