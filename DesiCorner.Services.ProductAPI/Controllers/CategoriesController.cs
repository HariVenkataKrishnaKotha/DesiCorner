using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Products;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DesiCorner.Services.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        IImageStorageService imageStorageService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(ct);
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = categories
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all categories");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving categories"
            });
        }
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id, ct);
            if (category == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = category
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving category"
            });
        }
    }

    /// <summary>
    /// Create new category with optional image (Admin only)
    /// Accepts multipart/form-data with category JSON and optional image file
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
    [FromForm] CategoryDto dto,
    IFormFile? image,
    CancellationToken ct)
    {
        try
        {
            // Upload image if provided
            if (image != null && image.Length > 0)
            {
                dto.ImageUrl = await _imageStorageService.UploadImageAsync(image, "categories", ct);
            }

            var category = await _categoryService.CreateCategoryAsync(dto, ct);

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new ResponseDto
            {
                IsSuccess = true,
                Message = "Category created successfully",
                Result = category
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
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error creating category"
            });
        }
    }

    /// <summary>
    /// Update category with optional image (Admin only)
    /// Accepts multipart/form-data with category JSON and optional image file
    /// If image is provided, old image is deleted and new one is uploaded
    /// If image is not provided, existing image is kept
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromForm] CategoryDto dto,
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
            // Get existing category to handle image
            var existingCategory = await _categoryService.GetCategoryByIdAsync(id, ct);
            if (existingCategory == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category not found"
                });
            }

            // Upload new image if provided
            if (image != null && image.Length > 0)
            {
                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingCategory.ImageUrl))
                {
                    await _imageStorageService.DeleteImageAsync(existingCategory.ImageUrl, ct);
                }

                // Upload new image
                dto.ImageUrl = await _imageStorageService.UploadImageAsync(image, "categories", ct);
            }
            else
            {
                // Keep existing image URL if no new image provided
                dto.ImageUrl = existingCategory.ImageUrl;
            }

            var category = await _categoryService.UpdateCategoryAsync(dto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Category updated successfully",
                Result = category
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
            _logger.LogError(ex, "Error updating category {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating category"
            });
        }
    }

    /// <summary>
    /// Update ONLY category image (Admin only)
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

            var existingCategory = await _categoryService.GetCategoryByIdAsync(id, ct);
            if (existingCategory == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category not found"
                });
            }

            // Delete old image
            if (!string.IsNullOrEmpty(existingCategory.ImageUrl))
            {
                await _imageStorageService.DeleteImageAsync(existingCategory.ImageUrl, ct);
            }

            // Upload new image
            var imageUrl = await _imageStorageService.UploadImageAsync(image, "categories", ct);

            // Update category with new image URL
            var updateDto = new CategoryDto
            {
                Id = id,
                Name = existingCategory.Name,
                Description = existingCategory.Description,
                ImageUrl = imageUrl,
                DisplayOrder = existingCategory.DisplayOrder
            };

            var updatedCategory = await _categoryService.UpdateCategoryAsync(updateDto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Image updated successfully",
                Result = updatedCategory
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
            _logger.LogError(ex, "Error updating image for category {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating image"
            });
        }
    }

    /// <summary>
    /// Delete category image (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/image")]
    public async Task<IActionResult> DeleteImage(Guid id, CancellationToken ct)
    {
        try
        {
            var existingCategory = await _categoryService.GetCategoryByIdAsync(id, ct);
            if (existingCategory == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category not found"
                });
            }

            if (string.IsNullOrEmpty(existingCategory.ImageUrl))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category has no image"
                });
            }

            // Delete image
            await _imageStorageService.DeleteImageAsync(existingCategory.ImageUrl, ct);

            // Update category to remove image URL
            var updateDto = new CategoryDto
            {
                Id = id,
                Name = existingCategory.Name,
                Description = existingCategory.Description,
                ImageUrl = null,
                DisplayOrder = existingCategory.DisplayOrder
            };

            var updatedCategory = await _categoryService.UpdateCategoryAsync(updateDto, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Image deleted successfully",
                Result = updatedCategory
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image for category {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error deleting image"
            });
        }
    }

    /// <summary>
    /// Delete category (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            // Get category to delete its image
            var category = await _categoryService.GetCategoryByIdAsync(id, ct);

            // Delete image if exists
            if (category?.ImageUrl != null)
            {
                await _imageStorageService.DeleteImageAsync(category.ImageUrl, ct);
            }

            var result = await _categoryService.DeleteCategoryAsync(id, ct);
            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Category not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Category deleted successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error deleting category"
            });
        }
    }
}