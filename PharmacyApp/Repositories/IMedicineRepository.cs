using PharmacyApp.Models;

namespace PharmacyApp.Repositories;

public interface IMedicineRepository
{
    List<Medicine> GetAll();
    Medicine? GetById(Guid id);
    Medicine Add(Medicine medicine);
    Medicine? Update(Guid id, Medicine medicine);
    bool Delete(Guid id);
}
