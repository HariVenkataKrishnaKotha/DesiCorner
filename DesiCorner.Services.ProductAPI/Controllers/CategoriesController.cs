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
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
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
    /// Create new category (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CategoryDto dto, CancellationToken ct)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, new ResponseDto
            {
                IsSuccess = true,
                Message = "Category created successfully",
                Result = category
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
    /// Update category (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryDto dto, CancellationToken ct)
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
            var category = await _categoryService.UpdateCategoryAsync(dto, ct);
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
                Message = "Category updated successfully",
                Result = category
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
    /// Delete category (Admin only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
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