using PharmacyApp.Models;
using PharmacyApp.Repositories;

namespace PharmacyApp.Services;

public class MedicineService : IMedicineService
{
    private readonly IMedicineRepository _repository;

    public MedicineService(IMedicineRepository repository)
    {
        _repository = repository;
    }

    public List<Medicine> GetAll(string? search = null)
    {
        var medicines = _repository.GetAll();
        if (!string.IsNullOrWhiteSpace(search))
            medicines = medicines.Where(m =>
                m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return medicines;
    }

    public Medicine? GetById(Guid id) => _repository.GetById(id);

    public Medicine Add(Medicine medicine)
    {
        medicine.Id = Guid.NewGuid();
        return _repository.Add(medicine);
    }

    public Medicine? Update(Guid id, Medicine medicine) => _repository.Update(id, medicine);

    public bool Delete(Guid id) => _repository.Delete(id);
}
