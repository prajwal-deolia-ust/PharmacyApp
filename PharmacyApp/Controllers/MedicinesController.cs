using Microsoft.AspNetCore.Mvc;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly MedicineService _service;

    public MedicinesController(MedicineService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll([FromQuery] string? search)
        => Ok(_service.GetAll(search));

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var med = _service.GetById(id);
        return med is null ? NotFound() : Ok(med);
    }

    [HttpPost]
    public IActionResult Create([FromBody] Medicine medicine)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = _service.Add(medicine);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] Medicine medicine)
    {
        var updated = _service.Update(id, medicine);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
        => _service.Delete(id) ? NoContent() : NotFound();
}
