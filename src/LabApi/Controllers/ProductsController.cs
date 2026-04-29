using LabApi.Data;
using LabApi.Contracts.Products;
using LabApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace LabApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private static readonly Counter _productOperationsTotal = Metrics
        .CreateCounter(
            "product_operations_total", 
            "Total product operations", 
            new CounterConfiguration
            {
                LabelNames = new[] { "operation", "status" }
            });
    
    private static readonly Histogram _productOperationDuration = Metrics
        .CreateHistogram(
            "product_operation_duration_seconds",
            "Duration of product operations",
            new HistogramConfiguration
            {
                Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0 },
                LabelNames = new[] { "operation" }
            });
    
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

        using (_productOperationDuration.WithLabels("get_all").NewTimer())
        {
            var products = await _context.Products
                .OrderBy(p => p.Id)
                .ToListAsync();
            
            _logger.LogDebug("Retrieved {ProductCount}  products", products.Count);
            _productOperationsTotal.WithLabels("get_all", "success").Inc();
            return Ok(products);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        _logger.LogInformation("Fetching product with ID {ProductId}", id);

        using (_productOperationDuration.WithLabels("get_by_id").NewTimer())
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found", id);
                return NotFound();
            }
            

            _logger.LogDebug("Product found: {@Product}", product);
            _productOperationsTotal.WithLabels("get_by_id", "success").Inc();
            
            return Ok(product);
        }

    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        _logger.LogInformation("Creating product: Name={ProductName}, Price={ProductPrice}",
            product.Name, product.Price);

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            _logger.LogWarning("Product creation failed: Name is empty");
            _productOperationsTotal.WithLabels("create", "validation_error").Inc();
            return BadRequest("Name is required");
        }

        if (product.Price < 0)
        {
            _logger.LogWarning("Product creation failed: Price {ProductPrice} is negative", product.Price);
            _productOperationsTotal.WithLabels("create", "validation_error").Inc();
            return BadRequest("Price must be 0 or higher.");
        }

        using (_productOperationDuration.WithLabels("create").NewTimer())
        {
            try
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Product created with ID {ProductId}", product.Id);
                _productOperationsTotal.WithLabels("create", "success").Inc();
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
            }
            catch(DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating product {ProductName}", product.Name);
                _productOperationsTotal.WithLabels("create", "error").Inc();
                return Problem("A database error occurred", statusCode: 500);
            }
        }
    }
}
