using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PharmacyApp.Controllers;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Controllers;

[TestClass]
public class SalesControllerTests
{
    private Mock<IWebHostEnvironment> _envMock = null!;
    private string _tempRoot = null!;
    private MedicineService _medicineService = null!;
    private SaleService _saleService = null!;
    private SalesController _controller = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "SalesControllerTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);
        _medicineService = new MedicineService(_envMock.Object);
        _saleService = new SaleService(_envMock.Object, _medicineService);
        _controller = new SalesController(_saleService);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // Constructor tests

    [TestMethod]
    public void Constructor_ValidService_CreatesInstance()
    {
        // Arrange & Act
        var controller = new SalesController(_saleService);

        // Assert
        Assert.IsNotNull(controller);
    }

    // GetAll tests

    [TestMethod]
    public void GetAll_NoSales_ReturnsOkResultWithEmptyList()
    {
        // Act
        var result = _controller.GetAll();

        // Assert
        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var value = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.IsEmpty(value);
    }

    [TestMethod]
    public void GetAll_WithRecordedSales_ReturnsOkResultWithAllSales()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 10,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        _saleService.RecordSale(new SaleRecord { MedicineId = medicine.Id, QuantitySold = 2 });

        // Act
        var result = _controller.GetAll();

        // Assert
        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var value = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.HasCount(1, value);
    }

    [TestMethod]
    public void GetAll_ReturnsOkObjectResultWith200StatusCode()
    {
        // Act
        var result = _controller.GetAll();

        // Assert
        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        Assert.AreEqual(200, ok.StatusCode);
    }

    // RecordSale tests

    [TestMethod]
    public void RecordSale_MedicineNotFound_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var sale = new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Medicine not found.", message);
    }

    [TestMethod]
    public void RecordSale_ZeroQuantity_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 10,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 0 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Quantity must be greater than zero.", message);
    }

    [TestMethod]
    public void RecordSale_NegativeQuantity_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 10,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = -5 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Quantity must be greater than zero.", message);
    }

    [TestMethod]
    public void RecordSale_InsufficientStock_ReturnsBadRequestWithMessage()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 5,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 10 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        var message = bad.Value?.GetType().GetProperty("message")?.GetValue(bad.Value)?.ToString();
        Assert.AreEqual("Insufficient stock. Available: 5", message);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReturnsCreatedAtActionResult()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 10,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 3 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var created = Assert.IsInstanceOfType<CreatedAtActionResult>(result);
        Assert.AreEqual(nameof(_controller.GetAll), created.ActionName);
        Assert.IsNotNull(created.Value);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReturnedSaleHasCorrectData()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Aspirin",
            Quantity = 10,
            Price = 5.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 3, CustomerName = "Alice" };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var created = Assert.IsInstanceOfType<CreatedAtActionResult>(result);
        var returnedSale = Assert.IsInstanceOfType<SaleRecord>(created.Value);
        Assert.AreEqual(medicine.Id, returnedSale.MedicineId);
        Assert.AreEqual(3, returnedSale.QuantitySold);
        Assert.AreEqual("Alice", returnedSale.CustomerName);
        Assert.AreEqual("Aspirin", returnedSale.MedicineName);
        Assert.AreEqual(5.0m, returnedSale.PricePerUnit);
    }

    [TestMethod]
    public void RecordSale_ValidSale_SaleAppearsInSubsequentGetAll()
    {
        // Arrange
        var medicine = _medicineService.Add(new Medicine
        {
            FullName = "Ibuprofen",
            Quantity = 20,
            Price = 8.0m,
            ExpiryDate = DateTime.Now.AddYears(1),
        });
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5 };

        // Act
        _controller.RecordSale(sale);
        var result = _controller.GetAll();

        // Assert
        var ok = Assert.IsInstanceOfType<OkObjectResult>(result);
        var sales = Assert.IsInstanceOfType<List<SaleRecord>>(ok.Value);
        Assert.HasCount(1, sales);
    }

    [TestMethod]
    public void RecordSale_BadRequest_Returns400StatusCode()
    {
        // Arrange
        var sale = new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 };

        // Act
        var result = _controller.RecordSale(sale);

        // Assert
        var bad = Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        Assert.AreEqual(400, bad.StatusCode);
    }
}
