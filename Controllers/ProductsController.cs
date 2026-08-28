using InventoryZeroAPI.DTOs.Products;
using InventoryZeroAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // GET api/products?search=jacket&categorySlug=clothing&page=1
        // All the filter params come from the URL query string automatically
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
        {
            try
            {
                var result = await _productService.GetAllAsync(filter);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                // Covers the page/pageSize guard clauses in ProductService -
                // anything else is unexpected and should still surface as a 500.
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET api/products/winter-jacket-cape-town
        // Slug is more SEO friendly than ID
        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBySlug(string slug)
        {
            var product = await _productService.GetBySlugAsync(slug);
            if (product == null)
                return NotFound(new { message = "Product not found." });
            return Ok(product);
        }

        // GET api/products/category/clothing
        [HttpGet("category/{categorySlug}")]
        public async Task<IActionResult> GetByCategory(string categorySlug)
        {
            var products = await _productService.GetByCategoryAsync(categorySlug);
            return Ok(products);
        }

        // GET api/products/shop/3
        [HttpGet("shop/{shopId}")]
        public async Task<IActionResult> GetByShop(int shopId)
        {
            var products = await _productService.GetByShopAsync(shopId);
            return Ok(products);
        }
    }
}