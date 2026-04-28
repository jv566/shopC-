using Microsoft.AspNetCore.Mvc;
using Shop.Contracts.Products;

namespace Shop.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<ProductDto>> Get()
    {
        var data = new List<ProductDto>
        {
            new(Guid.NewGuid(), "Keyboard", "KB-001", 199, "CNY"),
            new(Guid.NewGuid(), "Mouse", "MS-001", 99, "CNY")
        };

        return Ok(data);
    }
}

