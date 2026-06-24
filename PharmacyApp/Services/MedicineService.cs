using System.Text.Json;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class MedicineService
{
    private readonly string _dataPath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MedicineService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data", "medicines.json");
    }

    private List<Medicine> ReadAll()
    {
        if (!File.Exists(_dataPath)) return new List<Medicine>();
        var json = File.ReadAllText(_dataPath);
        return JsonSerializer.Deserialize<List<Medicine>>(json, _jsonOptions) ?? new List<Medicine>();
    }

    private void WriteAll(List<Medicine> medicines)
    {
        var json = JsonSerializer.Serialize(medicines, _jsonOptions);
        File.WriteAllText(_dataPath, json);
    }

    public List<Medicine> GetAll(string? search = null)
    {
        var medicines = ReadAll();
        if (!string.IsNullOrWhiteSpace(search))
            medicines = medicines.Where(m =>
                m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return medicines;
    }

    public Medicine? GetById(Guid id) => ReadAll().FirstOrDefault(m => m.Id == id);

    public Medicine Add(Medicine medicine)
    {
        var medicines = ReadAll();
        medicine.Id = Guid.NewGuid();
        medicines.Add(medicine);
        WriteAll(medicines);
        return medicine;
    }

    public Medicine? Update(Guid id, Medicine updated)
    {
        var medicines = ReadAll();
        var index = medicines.FindIndex(m => m.Id == id);
        if (index < 0) return null;
        updated.Id = id;
        medicines[index] = updated;
        WriteAll(medicines);
        return updated;
    }

    public bool Delete(Guid id)
    {
        var medicines = ReadAll();
        var removed = medicines.RemoveAll(m => m.Id == id);
        if (removed == 0) return false;
        WriteAll(medicines);
        return true;
    }
}
