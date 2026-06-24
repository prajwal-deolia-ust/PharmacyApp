using Moq;
using PharmacyApp.Models;
using PharmacyApp.Repositories;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Services;

[TestClass]
public class SaleServiceTests
{
    private Mock<ISaleRepository> _saleRepoMock = null!;
    private Mock<IMedicineService> _medicineServiceMock = null!;
    private SaleService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _saleRepoMock = new Mock<ISaleRepository>();
        _medicineServiceMock = new Mock<IMedicineService>();
        _service = new SaleService(_saleRepoMock.Object, _medicineServiceMock.Object);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NoSales_ReturnsEmptyList()
    {
        _saleRepoMock.Setup(r => r.GetAll()).Returns([]);

        var result = _service.GetAll();

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_AfterTwoSalesRecorded_ReturnsBothRecords()
    {
        _saleRepoMock.Setup(r => r.GetAll()).Returns(
        [
            new SaleRecord { Id = Guid.NewGuid() },
            new SaleRecord { Id = Guid.NewGuid() },
        ]);

        var result = _service.GetAll();

        Assert.HasCount(2, result);
    }

    // ── RecordSale – error paths ───────────────────────────────────────────────

    [TestMethod]
    public void RecordSale_MedicineNotFound_ReturnsNullRecordAndError()
    {
        _medicineServiceMock.Setup(s => s.GetById(It.IsAny<Guid>())).Returns((Medicine?)null);
        var sale = new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNull(record);
        Assert.AreEqual("Medicine not found.", error);
    }

    [TestMethod]
    public void RecordSale_ZeroQuantity_ReturnsNullRecordAndError()
    {
        var medicine = MakeMedicine(quantity: 20, price: 1.50m);
        _medicineServiceMock.Setup(s => s.GetById(medicine.Id)).Returns(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 0 };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNull(record);
        Assert.AreEqual("Quantity must be greater than zero.", error);
    }

    [TestMethod]
    public void RecordSale_NegativeQuantity_ReturnsNullRecordAndError()
    {
        var medicine = MakeMedicine(quantity: 20, price: 1.50m);
        _medicineServiceMock.Setup(s => s.GetById(medicine.Id)).Returns(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = -5 };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNull(record);
        Assert.AreEqual("Quantity must be greater than zero.", error);
    }

    [TestMethod]
    public void RecordSale_QuantityExceedsAvailableStock_ReturnsNullRecordAndError()
    {
        var medicine = MakeMedicine(quantity: 5, price: 2m);
        _medicineServiceMock.Setup(s => s.GetById(medicine.Id)).Returns(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 10 };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNull(record);
        Assert.AreEqual("Insufficient stock. Available: 5", error);
    }

    // ── RecordSale – success path ──────────────────────────────────────────────

    [TestMethod]
    public void RecordSale_ValidSale_ReturnsNonNullRecordWithNullError()
    {
        var medicine = MakeMedicine(quantity: 30, price: 8m, name: "Amoxicillin");
        SetupSuccessfulSale(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5, CustomerName = "Alice" };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNotNull(record);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void RecordSale_ValidSale_SaleFieldsAreAssignedFromMedicine()
    {
        var medicine = MakeMedicine(quantity: 30, price: 8m, name: "Amoxicillin");
        SetupSuccessfulSale(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5 };
        var beforeSale = DateTime.UtcNow;

        var (record, _) = _service.RecordSale(sale);

        Assert.IsNotNull(record);
        Assert.AreNotEqual(Guid.Empty, record.Id);
        Assert.AreEqual("Amoxicillin", record.MedicineName);
        Assert.AreEqual(8m, record.PricePerUnit);
        Assert.IsTrue(record.SaleDate >= beforeSale);
    }

    [TestMethod]
    public void RecordSale_ValidSale_ReducesMedicineQuantityBySoldAmount()
    {
        var medicine = MakeMedicine(quantity: 30, price: 8m, name: "Amoxicillin");
        SetupSuccessfulSale(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 7 };

        _service.RecordSale(sale);

        _medicineServiceMock.Verify(s =>
            s.Update(medicine.Id, It.Is<Medicine>(m => m.Quantity == 23)), Times.Once);
    }

    [TestMethod]
    public void RecordSale_ValidSale_PersistsSaleViaRepository()
    {
        var medicine = MakeMedicine(quantity: 30, price: 8m, name: "Amoxicillin");
        SetupSuccessfulSale(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 5, CustomerName = "Bob" };

        _service.RecordSale(sale);

        _saleRepoMock.Verify(r => r.Add(It.IsAny<SaleRecord>()), Times.Once);
    }

    [TestMethod]
    public void RecordSale_ExactAvailableQuantity_SucceedsAndDrainsStock()
    {
        var medicine = MakeMedicine(quantity: 10, price: 1m, name: "Vitamin C");
        SetupSuccessfulSale(medicine);
        var sale = new SaleRecord { MedicineId = medicine.Id, QuantitySold = 10 };

        var (record, error) = _service.RecordSale(sale);

        Assert.IsNotNull(record);
        Assert.IsNull(error);
        _medicineServiceMock.Verify(s =>
            s.Update(medicine.Id, It.Is<Medicine>(m => m.Quantity == 0)), Times.Once);
    }

    [TestMethod]
    public void RecordSale_MedicineNotFound_NeverCallsRepositoryAdd()
    {
        _medicineServiceMock.Setup(s => s.GetById(It.IsAny<Guid>())).Returns((Medicine?)null);

        _service.RecordSale(new SaleRecord { MedicineId = Guid.NewGuid(), QuantitySold = 1 });

        _saleRepoMock.Verify(r => r.Add(It.IsAny<SaleRecord>()), Times.Never);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Medicine MakeMedicine(int quantity, decimal price, string name = "TestMed") =>
        new() { Id = Guid.NewGuid(), FullName = name, Quantity = quantity, Price = price };

    private void SetupSuccessfulSale(Medicine medicine)
    {
        _medicineServiceMock.Setup(s => s.GetById(medicine.Id)).Returns(medicine);
        _medicineServiceMock.Setup(s => s.Update(medicine.Id, It.IsAny<Medicine>())).Returns(medicine);
        _saleRepoMock.Setup(r => r.Add(It.IsAny<SaleRecord>())).Returns<SaleRecord>(s => s);
    }
}
