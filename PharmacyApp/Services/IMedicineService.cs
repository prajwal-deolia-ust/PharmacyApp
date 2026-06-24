using PharmacyApp.Models;

namespace PharmacyApp.Services;

public interface IMedicineService
{
    List<Medicine> GetAll(string? search = null);
    Medicine? GetById(Guid id);
    Medicine Add(Medicine medicine);
    Medicine? Update(Guid id, Medicine medicine);
    bool Delete(Guid id);
}
