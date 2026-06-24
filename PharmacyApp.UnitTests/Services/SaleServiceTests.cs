using Microsoft.AspNetCore.Hosting;
using Moq;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Services;

[TestClass]
public class SaleServiceTests
{
    private string _tempDir = null!;
    private Mock<IWebHostEnvironment> _envMock = null!;
    private MedicineService _medicineService = null!;
    private SaleService _saleService = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempDir);

        _medicineService = new MedicineService(_envMock.Object);
        _saleService = new SaleService(_envMock.Object, _medicineService);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ── Constructor ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_ValidEnv_CreatesDataDirectory()
    {
        // Arrange — a brand-new root dir that has no Data sub-directory yet
        var freshDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(freshDir);

        try
        {
            var freshEnvMock = new Mock<IWebHostEnvironment>();
            freshEnvMock.Setup(e => e.ContentRootPath).Returns(freshDir);

            // Act
            _ = new SaleService(freshEnvMock.Object, _medicineService);

            // Assert
            Assert.IsTrue(Directory.Exists(Path.Combine(freshDir, "Data")));
        }
        finally
        {
            Directory.Delete(freshDir, true);
        }
    }

    [TestMethod]
    public void Constructor_ValidEnv_StoresMedicineServiceForSubsequentCalls()
    {
        // Verifying the medicine service is wired up: RecordSale must delegate to it.
        // If _medicineService were not stored, calling RecordSale would NullReferenceException.
        var sale = new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 };

        var (record, error) = _saleService.RecordSale(sale);

        // MedicineService returns null for an unknown id → proves the stored service is invoked.
        Assert.IsNull(record);
        Assert.AreEqual("Medicine not found.", error);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_WhenNoSalesFileExists_ReturnsEmptyList()
    {
        // Arrange — ensure sales.json is absent
        var salesPath = Path.Combine(_tempDir, "Data", "sales.json");
        if (File.Exists(salesPath))
            File.Delete(salesPath);

        // Act
        var result = _saleService.GetAll();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_AfterTwoSalesRecorded_ReturnsBothRecords()
    {
        // Arrange
        var medicine = AddMedicine("Ibuprofen", quantity: 100, price: 4.99m);
        _saleService.RecordSale(new SaleRecord { MedicineId = medicine.Id, QuantitySold = 2 });
        _saleService.RecordSale(new SaleRecord { MedicineId = medicine.Id, QuantitySold = 3 });

        // Act
        var result = _saleService.GetAll();

        // Assert
        Assert.HasCount(2, result);
    }

    // ── RecordSale – error paths ───────────────────────────────────────────────

    [TestMethod]
    public void RecordSale_MedicineNotFound_ReturnsNullRecordAndError()
    {
        // Arrange
        var sale = new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNull(record);
        Assert.AreEqual("Medicine not found.", error);
    }

    [TestMethod]
    public void RecordSale_ZeroQuantity_ReturnsNullRecordAndError()
    {
        // Arrange
        var medicine = AddMedicine("Aspirin", quantity: 20, price: 1.50m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 0 };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNull(record);
        Assert.AreEqual("Quantity must be greater than zero.", error);
    }

    [TestMethod]
    public void RecordSale_NegativeQuantity_ReturnsNullRecordAndError()
    {
        // Arrange
        var medicine = AddMedicine("Aspirin", quantity: 20, price: 1.50m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = -5 };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNull(record);
        Assert.AreEqual("Quantity must be greater than zero.", error);
    }

    [TestMethod]
    public void RecordSale_QuantityExceedsAvailableStock_ReturnsNullRecordAndError()
    {
        // Arrange
        var medicine = AddMedicine("Paracetamol", quantity: 5, price: 2m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 10 };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNull(record);
        Assert.AreEqual("Insufficient stock. Available: 5", error);
    }

    // ── RecordSale – success path ──────────────────────────────────────────────

    [TestMethod]
    public void RecordSale_ValidSale_ReturnsNonNullRecordWithNullError()
    {
        // Arrange
        var medicine = AddMedicine("Amoxicillin", quantity: 30, price: 8m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5, CustomerName = "Alice" };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNotNull(record);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void RecordSale_ValidSale_SaleFieldsAreAssigned()
    {
        // Arrange
        var medicine = AddMedicine("Amoxicillin", quantity: 30, price: 8m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5 };
        var beforeSale = DateTime.UtcNow;

        // Act
        var (record, _) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNotNull(record);
        Assert.AreNotEqual(Guid.Empty, record.Id);
        Assert.AreEqual("Amoxicillin", record.MedicineName);
        Assert.AreEqual(8m, record.PricePerUnit);
        Assert.IsTrue(record.SaleDate >= beforeSale);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReducesMedicineQuantityBySoldAmount()
    {
        // Arrange
        var medicine = AddMedicine("Amoxicillin", quantity: 30, price: 8m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 7 };

        // Act
        _saleService.RecordSale(sale);

        // Assert
        var updated = _medicineService.GetById(medicine.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual(23, updated.Quantity);
    }

    [TestMethod]
    public void RecordSale_ValidSale_PersistsSaleRecordRetrievableViaGetAll()
    {
        // Arrange
        var medicine = AddMedicine("Amoxicillin", quantity: 30, price: 8m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5, CustomerName = "Bob" };

        // Act
        var (recorded, _) = _saleService.RecordSale(sale);

        // Assert
        var allSales = _saleService.GetAll();
        Assert.HasCount(1, allSales);
        Assert.IsNotNull(recorded);
        Assert.AreEqual(recorded.Id, allSales[0].Id);
    }

    [TestMethod]
    public void RecordSale_ExactAvailableQuantity_SucceedsAndDrainsStock()
    {
        // Arrange — sell exactly what is in stock (boundary: quantity == available)
        var medicine = AddMedicine("Vitamin C", quantity: 10, price: 1m);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 10 };

        // Act
        var (record, error) = _saleService.RecordSale(sale);

        // Assert
        Assert.IsNotNull(record);
        Assert.IsNull(error);
        var updated = _medicineService.GetById(medicine.Id);
        Assert.IsNotNull(updated);
        Assert.AreEqual(0, updated.Quantity);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Medicine AddMedicine(string fullName, int quantity, decimal price) =>
        _medicineService.Add(new Medicine
        {
            FullName = fullName,
            Quantity = quantity,
            Price = price,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
        });
}
