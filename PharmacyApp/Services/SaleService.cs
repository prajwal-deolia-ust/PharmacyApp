using System.Text.Json;
using PharmacyApp.Models;

namespace PharmacyApp.Services;

public class SaleService
{
    private readonly string _salesPath;
    private readonly MedicineService _medicineService;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public SaleService(IWebHostEnvironment env, MedicineService medicineService)
    {
        _salesPath = Path.Combine(env.ContentRootPath, "Data", "sales.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_salesPath)!);
        _medicineService = medicineService;
    }

    private List<SaleRecord> ReadAll()
    {
        if (!File.Exists(_salesPath)) return new List<SaleRecord>();
        var json = File.ReadAllText(_salesPath);
        return JsonSerializer.Deserialize<List<SaleRecord>>(json, _jsonOptions) ?? new List<SaleRecord>();
    }

    private void WriteAll(List<SaleRecord> records)
    {
        var json = JsonSerializer.Serialize(records, _jsonOptions);
        File.WriteAllText(_salesPath, json);
    }

    public List<SaleRecord> GetAll()
    {
        _lock.Wait();
        try { return ReadAll(); }
        finally { _lock.Release(); }
    }

    public (SaleRecord? record, string? error) RecordSale(SaleRecord sale)
    {
        _lock.Wait();
        try
        {
            var medicine = _medicineService.GetById(sale.MedicineId);
            if (medicine == null) return (null, "Medicine not found.");
            if (sale.QuantitySold <= 0) return (null, "Quantity must be greater than zero.");
            if (sale.QuantitySold > medicine.Quantity) return (null, $"Insufficient stock. Available: {medicine.Quantity}");

            medicine.Quantity -= sale.QuantitySold;
            _medicineService.Update(medicine.Id, medicine);

            sale.Id = Guid.NewGuid();
            sale.MedicineName = medicine.FullName;
            sale.PricePerUnit = medicine.Price;
            sale.SaleDate = DateTime.UtcNow;

            var records = ReadAll();
            records.Add(sale);
            WriteAll(records);

            return (sale, null);
        }
        finally { _lock.Release(); }
    }
}
