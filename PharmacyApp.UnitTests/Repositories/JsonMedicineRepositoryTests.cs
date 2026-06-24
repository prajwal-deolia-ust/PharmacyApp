using Microsoft.AspNetCore.Hosting;
using Moq;
using PharmacyApp.Models;
using PharmacyApp.Repositories;

namespace PharmacyApp.UnitTests.Repositories;

[TestClass]
public class JsonMedicineRepositoryTests
{
    private Mock<IWebHostEnvironment> _envMock = null!;
    private string _tempRoot = null!;
    private JsonMedicineRepository _repo = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "JsonMedicineRepoTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);
        _repo = new JsonMedicineRepository(_envMock.Object);
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
        var act = () => new JsonMedicineRepository(_envMock.Object);
        act();
        Assert.IsTrue(Directory.Exists(Path.Combine(_tempRoot, "Data")));
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NoDataFile_ReturnsEmptyList()
    {
        var result = _repo.GetAll();

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_AfterTwoAdded_ReturnsBothMedicines()
    {
        _repo.Add(Medicine("Aspirin"));
        _repo.Add(Medicine("Ibuprofen"));

        var result = _repo.GetAll();

        Assert.HasCount(2, result);
    }

    // ── GetById ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetById_ExistingId_ReturnsMedicine()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        var result = _repo.GetById(added.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(added.Id, result.Id);
        Assert.AreEqual("Aspirin", result.FullName);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var result = _repo.GetById(Guid.NewGuid());

        Assert.IsNull(result);
    }

    // ── Add ────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Add_NewMedicine_AssignsNonEmptyGuid()
    {
        var result = _repo.Add(Medicine("Aspirin"));

        Assert.AreNotEqual(Guid.Empty, result.Id);
    }

    [TestMethod]
    public void Add_NewMedicine_PersistsToDisk()
    {
        _repo.Add(Medicine("Aspirin"));

        var all = _repo.GetAll();

        Assert.HasCount(1, all);
        Assert.AreEqual("Aspirin", all[0].FullName);
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Update_ExistingId_ReturnsUpdatedMedicine()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        var result = _repo.Update(added.Id, new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m });

        Assert.IsNotNull(result);
        Assert.AreEqual("Aspirin Extra", result.FullName);
        Assert.AreEqual(15, result.Quantity);
    }

    [TestMethod]
    public void Update_ExistingId_PreservesOriginalId()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        var result = _repo.Update(added.Id, new Medicine { FullName = "Aspirin Extra", Quantity = 5, Price = 3m });

        Assert.IsNotNull(result);
        Assert.AreEqual(added.Id, result.Id);
    }

    [TestMethod]
    public void Update_ExistingId_PersistsChangesToDisk()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        _repo.Update(added.Id, new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m });
        var found = _repo.GetById(added.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual("Aspirin Extra", found.FullName);
    }

    [TestMethod]
    public void Update_NonExistingId_ReturnsNull()
    {
        var result = _repo.Update(Guid.NewGuid(), new Medicine { FullName = "Ghost", Quantity = 1, Price = 1m });

        Assert.IsNull(result);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Delete_ExistingId_ReturnsTrue()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        var result = _repo.Delete(added.Id);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesFromDisk()
    {
        var added = _repo.Add(Medicine("Aspirin"));

        _repo.Delete(added.Id);

        Assert.IsEmpty(_repo.GetAll());
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesOnlyTargetMedicine()
    {
        var first  = _repo.Add(Medicine("Aspirin"));
        var second = _repo.Add(Medicine("Ibuprofen"));

        _repo.Delete(first.Id);
        var all = _repo.GetAll();

        Assert.HasCount(1, all);
        Assert.AreEqual(second.Id, all[0].Id);
    }

    [TestMethod]
    public void Delete_NonExistingId_ReturnsFalse()
    {
        var result = _repo.Delete(Guid.NewGuid());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Delete_AlreadyDeletedId_ReturnsFalse()
    {
        var added = _repo.Add(Medicine("Aspirin"));
        _repo.Delete(added.Id);

        var result = _repo.Delete(added.Id);

        Assert.IsFalse(result);
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static Medicine Medicine(string name) =>
        new() { FullName = name, Quantity = 10, Price = 5m, ExpiryDate = DateTime.UtcNow.AddYears(1) };
}
