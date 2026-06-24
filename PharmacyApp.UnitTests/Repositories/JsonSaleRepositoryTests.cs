using Microsoft.AspNetCore.Hosting;
using Moq;
using PharmacyApp.Models;
using PharmacyApp.Repositories;

namespace PharmacyApp.UnitTests.Repositories;

[TestClass]
public class JsonSaleRepositoryTests
{
    private Mock<IWebHostEnvironment> _envMock = null!;
    private string _tempRoot = null!;
    private JsonSaleRepository _repo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "JsonSaleRepoTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);
        _repo = new JsonSaleRepository(_envMock.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // ── Constructor ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_ValidEnv_CreatesDataDirectory()
    {
        Assert.IsTrue(Directory.Exists(Path.Combine(_tempRoot, "Data")));
    }

    [TestMethod]
    public void Constructor_DataDirectoryAlreadyExists_DoesNotThrow()
    {
        var act = () => new JsonSaleRepository(_envMock.Object);
        act();
        Assert.IsTrue(Directory.Exists(Path.Combine(_tempRoot, "Data")));
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NoSalesFile_ReturnsEmptyList()
    {
        var result = _repo.GetAll();

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_AfterTwoAdded_ReturnsBothRecords()
    {
        _repo.Add(Sale());
        _repo.Add(Sale());

        var result = _repo.GetAll();

        Assert.HasCount(2, result);
    }

    // ── Add ────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Add_NewRecord_PersistsToDisk()
    {
        var record = Sale();

        _repo.Add(record);

        var all = _repo.GetAll();
        Assert.HasCount(1, all);
    }

    [TestMethod]
    public void Add_NewRecord_ReturnsSameRecord()
    {
        var record = Sale();

        var result = _repo.Add(record);

        Assert.AreEqual(record.Id, result.Id);
    }

    [TestMethod]
    public void Add_MultipleRecords_AllPersisted()
    {
        _repo.Add(Sale());
        _repo.Add(Sale());
        _repo.Add(Sale());

        Assert.HasCount(3, _repo.GetAll());
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static SaleRecord Sale() =>
        new()
        {
            Id           = Guid.NewGuid(),
            MedicineId   = Guid.NewGuid(),
            MedicineName = "Aspirin",
            QuantitySold = 5,
            PricePerUnit = 2m,
            SaleDate     = DateTime.UtcNow,
        };
}
