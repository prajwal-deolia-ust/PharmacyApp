using Moq;
using PharmacyApp.Models;
using PharmacyApp.Repositories;
using PharmacyApp.Services;

namespace PharmacyApp.UnitTests.Services;

[TestClass]
public class MedicineServiceTests
{
    private Mock<IMedicineRepository> _repoMock = null!;
    private MedicineService _service = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _repoMock = new Mock<IMedicineRepository>();
        _service = new MedicineService(_repoMock.Object);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetAll_NoMedicines_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAll()).Returns([]);

        var result = _service.GetAll();

        Assert.IsNotNull(result);
        Assert.IsEmpty(result);
    }

    [TestMethod]
    public void GetAll_NullSearch_ReturnsAllMedicines()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin",   Quantity = 10, Price = 5.0m },
            new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m },
        ]);

        var result = _service.GetAll(null);

        Assert.HasCount(2, result);
    }

    [TestMethod]
    public void GetAll_EmptySearch_ReturnsAllMedicines()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m },
        ]);

        var result = _service.GetAll(string.Empty);

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void GetAll_WhitespaceSearch_ReturnsAllMedicines()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m },
        ]);

        var result = _service.GetAll("   ");

        Assert.HasCount(1, result);
    }

    [TestMethod]
    public void GetAll_SearchMatchesSome_ReturnsFilteredList()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin",   Quantity = 10, Price = 5.0m },
            new Medicine { FullName = "Ibuprofen", Quantity = 20, Price = 8.0m },
        ]);

        var result = _service.GetAll("aspirin");

        Assert.HasCount(1, result);
        Assert.AreEqual("Aspirin", result[0].FullName);
    }

    [TestMethod]
    public void GetAll_SearchIsCaseInsensitive_ReturnsMatchingMedicines()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin Plus", Quantity = 10, Price = 5.0m },
        ]);

        var result = _service.GetAll("ASPIRIN");

        Assert.HasCount(1, result);
        Assert.AreEqual("Aspirin Plus", result[0].FullName);
    }

    [TestMethod]
    public void GetAll_SearchMatchesNone_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetAll()).Returns(
        [
            new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m },
        ]);

        var result = _service.GetAll("Paracetamol");

        Assert.IsEmpty(result);
    }

    // ── GetById ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void GetById_ExistingId_ReturnsMedicine()
    {
        var id = Guid.NewGuid();
        var medicine = new Medicine { Id = id, FullName = "Aspirin" };
        _repoMock.Setup(r => r.GetById(id)).Returns(medicine);

        var result = _service.GetById(id);

        Assert.IsNotNull(result);
        Assert.AreEqual(id, result.Id);
        Assert.AreEqual("Aspirin", result.FullName);
    }

    [TestMethod]
    public void GetById_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetById(It.IsAny<Guid>())).Returns((Medicine?)null);

        var result = _service.GetById(Guid.NewGuid());

        Assert.IsNull(result);
    }

    // ── Add ────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Add_NewMedicine_AssignsNewGuidBeforePassingToRepository()
    {
        var medicine = new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m };
        _repoMock.Setup(r => r.Add(It.IsAny<Medicine>())).Returns<Medicine>(m => m);

        var result = _service.Add(medicine);

        Assert.AreNotEqual(Guid.Empty, result.Id);
        _repoMock.Verify(r => r.Add(It.Is<Medicine>(m => m.Id != Guid.Empty)), Times.Once);
    }

    [TestMethod]
    public void Add_NewMedicine_DelegatesToRepository()
    {
        var medicine = new Medicine { FullName = "Aspirin", Quantity = 10, Price = 5.0m };
        _repoMock.Setup(r => r.Add(It.IsAny<Medicine>())).Returns<Medicine>(m => m);

        _service.Add(medicine);

        _repoMock.Verify(r => r.Add(It.IsAny<Medicine>()), Times.Once);
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Update_ExistingId_ReturnsUpdatedMedicine()
    {
        var id = Guid.NewGuid();
        var updated = new Medicine { FullName = "Aspirin Extra", Quantity = 15, Price = 6.5m };
        _repoMock.Setup(r => r.Update(id, updated)).Returns(updated);

        var result = _service.Update(id, updated);

        Assert.IsNotNull(result);
        Assert.AreEqual("Aspirin Extra", result.FullName);
    }

    [TestMethod]
    public void Update_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.Update(It.IsAny<Guid>(), It.IsAny<Medicine>())).Returns((Medicine?)null);

        var result = _service.Update(Guid.NewGuid(), new Medicine());

        Assert.IsNull(result);
    }

    [TestMethod]
    public void Update_AnyId_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        var medicine = new Medicine();
        _repoMock.Setup(r => r.Update(id, medicine)).Returns(medicine);

        _service.Update(id, medicine);

        _repoMock.Verify(r => r.Update(id, medicine), Times.Once);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Delete_ExistingId_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.Delete(id)).Returns(true);

        var result = _service.Delete(id);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Delete_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.Delete(It.IsAny<Guid>())).Returns(false);

        var result = _service.Delete(Guid.NewGuid());

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Delete_AnyId_DelegatesToRepository()
    {
        var id = Guid.NewGuid();
        _repoMock.Setup(r => r.Delete(id)).Returns(true);

        _service.Delete(id);

        _repoMock.Verify(r => r.Delete(id), Times.Once);
    }
}
