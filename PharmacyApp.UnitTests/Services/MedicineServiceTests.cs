using Microsoft.AspNetCore.Hosting;
using Moq;
using PharmacyApp.Models;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Services;

[TestClass]
public class MedicineServiceTests
{
    private Mock<IWebHostEnvironment> _envMock = null!;
    private string _tempRoot = null!;
    private MedicineService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "MedicineServiceTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempRoot);
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(_tempRoot);
        _service = new MedicineService(_envMock.Object);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // Constructor tests

    [TestMethod]
    public void Constructor_ValidEnv_CreatesDataDirectory()
    {
        var expectedDir = Path.Combine(_tempRoot, "Data");
        Assert.IsTrue(Directory.Exists(expectedDir));
    }

    [TestMethod]
    public void Constructor_DataDirectoryAlreadyExists_DoesNotThrow()
    {
        // Data dir already created by TestInitialize; creating service again must not throw
        var act = () => new MedicineService(_envMock.Object);
        act();
        Assert.IsTrue(Directory.Exists(Path.Combine(_tempRoot, "Data")));
    }

    // GetAll tests

    [TestMethod]
    public void GetAll_NoDataFile_ReturnsEmptyList()
    {
        var result = _service.GetAll();

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_NullSearch_ReturnsAllMedicines()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        _service.Add(new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll(null);

        Assert.HasCount(2, result);
    }

    [TestMethod]
    public void GetAll_EmptySearch_ReturnsAllMedicines()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll(string.Empty);

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void GetAll_WhitespaceSearch_ReturnsAllMedicines()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll("   ");

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void GetAll_SearchMatchesSome_ReturnsFilteredList()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        _service.Add(new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll("aspirin");

        Assert.HasCount(1, result);
        Assert.AreEqual("Aspirin", result[0].FullName);
    }

    [TestMethod]
    public void GetAll_SearchIsCaseInsensitive_ReturnsMatchingMedicines()
    {
        _service.Add(new Medicine { FullName = "Aspirin Plus", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll("ASPIRIN");

        Assert.HasCount(1, result);
        Assert.AreEqual("Aspirin Plus", result[0].FullName);
    }

    [TestMethod]
    public void GetAll_SearchMatchesNone_ReturnsEmptyList()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetAll("Paracetamol");

        Assert.IsEmpty(result);
    }

    // GetById tests

    [TestMethod]
    public void GetById_ExistingId_ReturnsMedicine()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.GetById(added.Id);

        Assert.IsNotNull(result);
        Assert.AreEqual(added.Id, result.Id);
        Assert.AreEqual("Aspirin", result.FullName);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNull()
    {
        var result = _service.GetById(Guid.NewGuid());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetById_EmptyStore_ReturnsNull()
    {
        var result = _service.GetById(Guid.NewGuid());

        Assert.IsNull(result);
    }

    // Add tests

    [TestMethod]
    public void Add_NewMedicine_AssignsNonEmptyGuid()
    {
        var medicine = new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) };

        var result = _service.Add(medicine);

        Assert.AreNotEqual(Guid.Empty, result.Id);
    }

    [TestMethod]
    public void Add_NewMedicine_PersistsMedicineToFile()
    {
        var medicine = new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) };

        _service.Add(medicine);
        var all = _service.GetAll();

        Assert.HasCount(1, all);
        Assert.AreEqual("Aspirin", all[0].FullName);
    }

    [TestMethod]
    public void Add_NewMedicine_ReturnsMedicineWithAssignedId()
    {
        var medicine = new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) };

        var result = _service.Add(medicine);

        Assert.IsNotNull(result);
        Assert.AreEqual(medicine.Id, result.Id);
        Assert.AreEqual("Aspirin", result.FullName);
    }

    [TestMethod]
    public void Add_MultipleMedicines_AllPersisted()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        _service.Add(new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var all = _service.GetAll();

        Assert.HasCount(2, all);
    }

    // Update tests

    [TestMethod]
    public void Update_NonExistingId_ReturnsNull()
    {
        var result = _service.Update(Guid.NewGuid(), new Medicine { FullName = "Updated", Quantity = 5, Price = 3.0m });

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Update_ExistingId_ReturnsUpdatedMedicine()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        var updated = new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m, ExpiryDate = DateTime.Now.AddYears(2) };

        var result = _service.Update(added.Id, updated);

        Assert.IsNotNull(result);
        Assert.AreEqual("Aspirin Extra", result.FullName);
        Assert.AreEqual(15, result.Quantity);
    }

    [TestMethod]
    public void Update_ExistingId_SetsCorrectIdOnUpdatedMedicine()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        var updated = new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m };

        var result = _service.Update(added.Id, updated);

        Assert.IsNotNull(result);
        Assert.AreEqual(added.Id, result.Id);
    }

    [TestMethod]
    public void Update_ExistingId_PersistsChangesToFile()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        var updated = new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m };

        _service.Update(added.Id, updated);
        var found = _service.GetById(added.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual("Aspirin Extra", found.FullName);
        Assert.AreEqual(15, found.Quantity);
    }

    [TestMethod]
    public void Update_NonExistingId_DoesNotModifyExistingData()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        _service.Update(Guid.NewGuid(), new Medicine { FullName = "Fake", Quantity = 99, Price = 1.0m });
        var all = _service.GetAll();

        Assert.HasCount(1, all);
        Assert.AreEqual("Aspirin", all[0].FullName);
    }

    // Delete tests

    [TestMethod]
    public void Delete_ExistingId_ReturnsTrue()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.Delete(added.Id);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_EmptyStore_ReturnsFalse()
    {
        var result = _service.Delete(Guid.NewGuid());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_WithData_ReturnsFalse()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        var result = _service.Delete(Guid.NewGuid());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesMedicineFromStore()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        _service.Delete(added.Id);
        var all = _service.GetAll();

        Assert.IsEmpty(all);
    }

    [TestMethod]
    public void Delete_ExistingId_RemovesOnlyTargetMedicine()
    {
        var first = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        var second = _service.Add(new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        _service.Delete(first.Id);
        var all = _service.GetAll();

        Assert.HasCount(1, all);
        Assert.AreEqual(second.Id, all[0].Id);
        Assert.AreEqual("Ibuprofen", all[0].FullName);
    }

    [TestMethod]
    public void Delete_ExistingId_MedicineNoLongerFoundById()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        _service.Delete(added.Id);
        var found = _service.GetById(added.Id);

        Assert.IsNull(found);
    }

    [TestMethod]
    public void Delete_AlreadyDeletedId_ReturnsFalse()
    {
        var added = _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        _service.Delete(added.Id);

        var result = _service.Delete(added.Id);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_DoesNotModifyExistingData()
    {
        _service.Add(new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m, ExpiryDate = DateTime.Now.AddYears(1) });
        _service.Add(new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m, ExpiryDate = DateTime.Now.AddYears(1) });

        _service.Delete(Guid.NewGuid());
        var all = _service.GetAll();

        Assert.HasCount(2, all);
    }
}
