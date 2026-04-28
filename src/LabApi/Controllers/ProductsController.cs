using LabApi.Data;
using LabApi.Contracts.Products;
using LabApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext context, ILogger<ProductsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        _logger.LogInformation("Fetching all products");
        
        var products = await _context.Products
            .OrderBy(p => p.Id)
            .ToListAsync();
        
        _logger.LogDebug("Retrieved {ProductCount}  products", products.Count);

        return Ok(products);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        _logger.LogInformation("Fetching product with ID {ProductId}", id);

        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return NotFound();
        }
        

        _logger.LogDebug("Product found: {@Product}", product);
        
        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        _logger.LogInformation("Creating product: Name={ProductName}, Price={ProductPrice}",
            product.Name, product.Price);

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            _logger.LogWarning("Product creation failed: Name is empty");
            return BadRequest("Name is required");
        }

        if (product.Price < 0)
        {
            _logger.LogWarning("Product creation failed: Price {ProductPrice} is negative", product.Price);
            return BadRequest("Price must be 0 or higher.");
        }

        try
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created with ID {ProductId}", product.Id);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch(DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating product {ProductName}", product.Name);
            return Problem("A database error occurred", statusCode: 500);
        }
    }
}
