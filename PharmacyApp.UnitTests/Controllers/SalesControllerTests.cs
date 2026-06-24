using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmacyApp.Controllers;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Controllers;

[TestClass]
public class SalesControllerTests
{
    private Mock<ISaleService> _serviceMock = null!;
    private SalesController _controller = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _serviceMock = new Mock<ISaleService>();
        _controller = new SalesController(_serviceMock.Object);
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_ValidService_CreatesInstance()
    {
        var controller = new SalesController(_serviceMock.Object);

        Assert.IsNotNull(controller);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NoSales_ReturnsOkResultWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAll()).Returns([]);

        var result = _controller.GetAll();

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var value = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.IsEmpty(value);
    }

    [TestMethod]
    public void GetAll_WithRecordedSales_ReturnsOkResultWithAllSales()
    {
        _serviceMock.Setup(s => s.GetAll()).Returns(
        [
            new SaleRecord { Id = Guid.NewGuid(), MedicineName = "Aspirin", QuantitySold = 2 },
        ]);

        var result = _controller.GetAll();

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var value = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.HasCount(1, value);
    }

    [TestMethod]
    public void GetAll_ReturnsOkObjectResultWith200StatusCode()
    {
        _serviceMock.Setup(s => s.GetAll()).Returns([]);

        var result = _controller.GetAll();

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.AreEqual(200, ok.StatusCode);
    }

    // ── RecordSale ────────────────────────────────────────────────────────────

    [TestMethod]
    public void RecordSale_MedicineNotFound_ReturnsBadRequestWithMessage()
    {
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>()))
                    .Returns((null, "Medicine not found."));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 });

        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Medicine not found.", message);
    }

    [TestMethod]
    public void RecordSale_ZeroQuantity_ReturnsBadRequestWithMessage()
    {
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>()))
                    .Returns((null, "Quantity must be greater than zero."));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 0 });

        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Quantity must be greater than zero.", message);
    }

    [TestMethod]
    public void RecordSale_NegativeQuantity_ReturnsBadRequestWithMessage()
    {
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>()))
                    .Returns((null, "Quantity must be greater than zero."));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = -5 });

        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Quantity must be greater than zero.", message);
    }

    [TestMethod]
    public void RecordSale_InsufficientStock_ReturnsBadRequestWithMessage()
    {
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>()))
                    .Returns((null, "Insufficient stock. Available: 5"));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 10 });

        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Insufficient stock. Available: 5", message);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReturnsCreatedAtActionResult()
    {
        var medicineId = Guid.NewGuid();
        var recorded = new SaleRecord { Id = Guid.NewGuid(), MedicineId = medicineId, MedicineName = "Aspirin", QuantitySold = 3, PricePerUnit = 5.0m };
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>())).Returns((recorded, null));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = medicineId, QuantitySold = 3 });

        var created = Assert.IsInstanceOfType<CreatedAtActionResult>(result);
        Assert.AreEqual(nameof(_controller.GetAll), created.ActionName);
        Assert.IsNotNull(created.Value);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReturnedSaleHasCorrectData()
    {
        var medicineId = Guid.NewGuid();
        var recorded = new SaleRecord
        {
            Id           = Guid.NewGuid(),
            MedicineId   = medicineId,
            MedicineName = "Aspirin",
            QuantitySold = 3,
            PricePerUnit = 5.0m,
            CustomerName = "Alice",
        };
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>())).Returns((recorded, null));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = medicineId, QuantitySold = 3, CustomerName = "Alice" });

        var created = Assert.IsInstanceOfType<CreatedAtActionResult>(result);
        var returnedSale = Assert.IsInstanceOfType<SaleRecord>(created.Value);
        Assert.AreEqual(medicineId, returnedSale.MedicineId);
        Assert.AreEqual(3, returnedSale.QuantitySold);
        Assert.AreEqual("Alice", returnedSale.CustomerName);
        Assert.AreEqual("Aspirin", returnedSale.MedicineName);
        Assert.AreEqual(5.0m, returnedSale.PricePerUnit);
    }

    [TestMethod]
    public void RecordSale_ValidSale_SaleAppearsInSubsequentGetAll()
    {
        var medicineId = Guid.NewGuid();
        var recorded = new SaleRecord { Id = Guid.NewGuid(), MedicineId = medicineId, QuantitySold = 5 };
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>())).Returns((recorded, null));
        _serviceMock.Setup(s => s.GetAll()).Returns([recorded]);

        _controller.RecordSale(new SaleRecord { MedicineId = medicineId, QuantitySold = 5 });
        var result = _controller.GetAll();

        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var sales = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.HasCount(1, sales);
    }

    [TestMethod]
    public void RecordSale_BadRequest_Returns400StatusCode()
    {
        _serviceMock.Setup(s => s.RecordSale(It.IsAny<SaleRecord>()))
                    .Returns((null, "Medicine not found."));

        var result = _controller.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 });

        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        Assert.AreEqual(400, bad.StatusCode);
    }
}
