using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmacyApp.Controllers;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests;

[TestClass]
public class MedicinesControllerTests
{
    private Mock<IMedicineService> _serviceMock = null!;
    private MedicinesController _controller = null!;

    [TestInitialize]
    public void Initialize()
    {
        _serviceMock = new Mock<IMedicineService>();
        _controller = new MedicinesController(_serviceMock.Object);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_WithService_CreatesInstance()
    {
        var controller = new MedicinesController(_serviceMock.Object);

        Assert.IsNotNull(controller);
    }

    // ── GetAll ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NullSearch_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAll(null)).Returns([]);

        var result = _controller.GetAll(null);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.IsEmpty(list);
    }

    [TestMethod]
    public void GetAll_NullSearch_WithExistingMedicines_ReturnsOkWithAllMedicines()
    {
        _serviceMock.Setup(s => s.GetAll(null)).Returns(
        [
            CreateMedicine("Aspirin"),
            CreateMedicine("Ibuprofen"),
        ]);

        var result = _controller.GetAll(null);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var list = ok.Value as List<Medicine>;
        Assert.IsNotNull(list);
        Assert.HasCount(2, list);
    }

    [TestMethod]
    public void GetAll_MatchingSearch_ReturnsOkWithFilteredList()
    {
        _serviceMock.Setup(s => s.GetAll("Aspirin")).Returns([CreateMedicine("Aspirin")]);

        var result = _controller.GetAll("Aspirin");

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
        _serviceMock.Setup(s => s.GetAll("xyz-not-found")).Returns([]);

        var result = _controller.GetAll("xyz-not-found");

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
        var medicine = CreateMedicine("Paracetamol");
        _serviceMock.Setup(s => s.GetById(medicine.Id)).Returns(medicine);

        var result = _controller.GetById(medicine.Id);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var returned = ok.Value as Medicine;
        Assert.IsNotNull(returned);
        Assert.AreEqual(medicine.Id, returned.Id);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetById(It.IsAny<Guid>())).Returns((Medicine?)null);

        var result = _controller.GetById(Guid.NewGuid());

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Create_InvalidModelState_ReturnsBadRequest()
    {
        _controller.ModelState.AddModelError("FullName", "Required");

        var result = _controller.Create(new Medicine());

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void Create_ValidMedicine_ReturnsCreatedAtActionResult()
    {
        var medicine = CreateMedicine("Ibuprofen");
        _serviceMock.Setup(s => s.Add(medicine)).Returns(medicine);

        var result = _controller.Create(medicine);

        Assert.IsInstanceOfType<CreatedAtActionResult>(result);
    }

    [TestMethod]
    public void Create_ValidMedicine_ActionNamePointsToGetById()
    {
        var medicine = CreateMedicine("Ibuprofen");
        _serviceMock.Setup(s => s.Add(medicine)).Returns(medicine);

        var result = (CreatedAtActionResult)_controller.Create(medicine);

        Assert.AreEqual(nameof(MedicinesController.GetById), result.ActionName);
    }

    [TestMethod]
    public void Create_ValidMedicine_RouteValuesContainCreatedId()
    {
        var medicine = CreateMedicine("Ibuprofen");
        _serviceMock.Setup(s => s.Add(medicine)).Returns(medicine);

        var result = (CreatedAtActionResult)_controller.Create(medicine);

        var returnedMed = result.Value as Medicine;
        Assert.IsNotNull(returnedMed);
        Assert.IsNotNull(result.RouteValues);
        Assert.AreEqual(returnedMed.Id.ToString(), result.RouteValues["id"]?.ToString());
    }

    [TestMethod]
    public void Create_ValidMedicine_BodyContainsCreatedMedicine()
    {
        var medicine = CreateMedicine("Ibuprofen");
        _serviceMock.Setup(s => s.Add(medicine)).Returns(medicine);

        var result = (CreatedAtActionResult)_controller.Create(medicine);

        var returnedMed = result.Value as Medicine;
        Assert.IsNotNull(returnedMed);
        Assert.AreEqual("Ibuprofen", returnedMed.FullName);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Update_ExistingId_ReturnsOkWithUpdatedMedicine()
    {
        var id = Guid.NewGuid();
        var updated = CreateMedicine("NewName");
        _serviceMock.Setup(s => s.Update(id, updated)).Returns(updated);

        var result = _controller.Update(id, updated);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var medicine = ok.Value as Medicine;
        Assert.IsNotNull(medicine);
        Assert.AreEqual("NewName", medicine.FullName);
    }

    [TestMethod]
    public void Update_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.Update(It.IsAny<Guid>(), It.IsAny<Medicine>())).Returns((Medicine?)null);

        var result = _controller.Update(Guid.NewGuid(), CreateMedicine("Any"));

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Delete ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Delete_ExistingId_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.Delete(id)).Returns(true);

        var result = _controller.Delete(id);

        Assert.IsInstanceOfType<NoContentResult>(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.Delete(It.IsAny<Guid>())).Returns(false);

        var result = _controller.Delete(Guid.NewGuid());

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static Medicine CreateMedicine(string fullName) => new()
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        ExpiryDate = DateTime.UtcNow.AddYears(1),
        Quantity = 10,
        Price = 9.99m,
        Brand = "TestBrand",
    };
}
