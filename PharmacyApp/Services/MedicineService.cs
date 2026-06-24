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
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MedicineService(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data", "medicines.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_dataPath)!);
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
        _lock.Wait();
        try
        {
            var medicines = ReadAll();
            if (!string.IsNullOrWhiteSpace(search))
                medicines = medicines.Where(m =>
                    m.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
            return medicines;
        }
        finally { _lock.Release(); }
    }

    public Medicine? GetById(Guid id)
    {
        _lock.Wait();
        try { return ReadAll().FirstOrDefault(m => m.Id == id); }
        finally { _lock.Release(); }
    }

    public Medicine Add(Medicine medicine)
    {
        _lock.Wait();
        try
        {
            var medicines = ReadAll();
            medicine.Id = Guid.NewGuid();
            medicines.Add(medicine);
            WriteAll(medicines);
            return medicine;
        }
        finally { _lock.Release(); }
    }

    public Medicine? Update(Guid id, Medicine updated)
    {
        _lock.Wait();
        try
        {
            var medicines = ReadAll();
            var index = medicines.FindIndex(m => m.Id == id);
            if (index < 0) return null;
            updated.Id = id;
            medicines[index] = updated;
            WriteAll(medicines);
            return updated;
        }
        finally { _lock.Release(); }
    }

    public bool Delete(Guid id)
    {
        _lock.Wait();
        try
        {
            var medicines = ReadAll();
            var removed = medicines.RemoveAll(m => m.Id == id);
            if (removed == 0) return false;
            WriteAll(medicines);
            return true;
        }
        finally { _lock.Release(); }
    }
}
