using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly ISaleService _service;

    public SalesController(ISaleService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll() => Ok(_service.GetAll());

    [HttpPost]
    public IActionResult RecordSale([FromBody] SaleRecord sale)
    {
        var (record, error) = _service.RecordSale(sale);
        if (error != null) return BadRequest(new { message = error });
        return CreatedAtAction(nameof(GetAll), record);
    }
}
