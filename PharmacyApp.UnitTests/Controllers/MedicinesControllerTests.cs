using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmacyApp.Controllers;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests;

[TestClass]
public class MedicinesControllerTests
{
    private string _tempDir = string.Empty;
    private MedicineService _service = null!;
    private MedicinesController _controller = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);
        _service = new MedicineService(mockEnv.Object);
        _controller = new MedicinesController(_service);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_WithService_CreatesInstance()
    {
        // Arrange
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(_tempDir);
        var service = new MedicineService(mockEnv.Object);

        // Act
        var controller = new MedicinesController(service);

        // Assert
        Assert.IsNotNull(controller);
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NullSearch_WhenEmpty_ReturnsOkWithEmptyList()
    {
        // Act
        var result = _controller.GetAll(null);

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.IsEmpty(list);
    }

    [TestMethod]
    public void GetAll_NullSearch_WithExistingMedicines_ReturnsOkWithAllMedicines()
    {
        // Arrange
        _service.Add(CreateMedicine("Aspirin"));
        _service.Add(CreateMedicine("Ibuprofen"));

        // Act
        var result = _controller.GetAll(null);

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.HasCount(2, list);
    }

    [TestMethod]
    public void GetAll_MatchingSearch_ReturnsOkWithFilteredList()
    {
        // Arrange
        _service.Add(CreateMedicine("Aspirin"));
        _service.Add(CreateMedicine("Ibuprofen"));

        // Act
        var result = _controller.GetAll("Aspirin");

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.HasCount(1, list);
        Assert.AreEqual("Aspirin", list[0].FullName);
    }

    [TestMethod]
    public void GetAll_NonMatchingSearch_ReturnsOkWithEmptyList()
    {
        // Arrange
        _service.Add(CreateMedicine("Aspirin"));

        // Act
        var result = _controller.GetAll("xyz-not-found");

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.IsEmpty(list);
    }

    // ── GetById ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetById_ExistingId_ReturnsOkWithMedicine()
    {
        // Arrange
        var added = _service.Add(CreateMedicine("Paracetamol"));

        // Act
        var result = _controller.GetById(added.Id);

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var medicine = ok.Value as Medicine;
        Assert.IsNotNull(medicine);
        Assert.AreEqual(added.Id, medicine.Id);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNotFound()
    {
        // Act
        var result = _controller.GetById(Guid.NewGuid());

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        _controller.ModelState.AddModelError("FullName", "Required");

        // Act
        var result = _controller.Create(new Medicine());

        // Assert
        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void Create_ValidMedicine_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var medicine = CreateMedicine("Ibuprofen");

        // Act
        var result = _controller.Create(medicine);

        // Assert
        Assert.IsInstanceOfType<CreatedAtActionResult>(result);
    }

    [TestMethod]
    public void Create_ValidMedicine_ActionNamePointsToGetById()
    {
        // Arrange
        var medicine = CreateMedicine("Ibuprofen");

        // Act
        var result = (CreatedAtActionResult)_controller.Create(medicine);

        // Assert
        Assert.AreEqual(nameof(MedicinesController.GetById), result.ActionName);
    }

    [TestMethod]
    public void Create_ValidMedicine_RouteValuesContainCreatedId()
    {
        // Arrange
        var medicine = CreateMedicine("Ibuprofen");

        // Act
        var result = (CreatedAtActionResult)_controller.Create(medicine);

        // Assert
        var returnedMed = result.Value as Medicine;
        Assert.IsNotNull(returnedMed);
        Assert.IsNotNull(result.RouteValues);
        Assert.AreEqual(returnedMed.Id.ToString(), result.RouteValues["id"]?.ToString());
    }

    [TestMethod]
    public void Create_ValidMedicine_BodyContainsCreatedMedicine()
    {
        // Arrange
        var medicine = CreateMedicine("Ibuprofen");

        // Act
        var result = (CreatedAtActionResult)_controller.Create(medicine);

        // Assert
        var returnedMed = result.Value as Medicine;
        Assert.IsNotNull(returnedMed);
        Assert.AreEqual("Ibuprofen", returnedMed.FullName);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Update_ExistingId_ReturnsOkWithUpdatedMedicine()
    {
        // Arrange
        var added = _service.Add(CreateMedicine("OldName"));
        var update = CreateMedicine("NewName");

        // Act
        var result = _controller.Update(added.Id, update);

        // Assert
        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var updated = ok.Value as Medicine;
        Assert.IsNotNull(updated);
        Assert.AreEqual("NewName", updated.FullName);
    }

    [TestMethod]
    public void Update_NonExistingId_ReturnsNotFound()
    {
        // Act
        var result = _controller.Update(Guid.NewGuid(), CreateMedicine("Any"));

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Delete_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var added = _service.Add(CreateMedicine("Amoxicillin"));

        // Act
        var result = _controller.Delete(added.Id);

        // Assert
        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_ReturnsNotFound()
    {
        // Act
        var result = _controller.Delete(Guid.NewGuid());

        // Assert
        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static Medicine CreateMedicine(string fullName) => new()
    {
        FullName = fullName,
        ExpiryDate = DateTime.UtcNow.AddYears(1),
        Quantity = 10,
        Price = 9.99m,
        Brand = "TestBrand",
    };
}
