using System.Text.Json;
using PharmacyApp.Models;

namespace PharmacyApp.Repositories;

public class JsonSaleRepository : ISaleRepository
{
    private readonly string _salesPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public JsonSaleRepository(IWebHostEnvironment env)
    {
        _salesPath = Path.Combine(env.ContentRootPath, "Data", "sales.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_salesPath)!);
    }

    private List<SaleRecord> ReadAll()
    {
        if (!File.Exists(_salesPath)) return [];
        var json = File.ReadAllText(_salesPath);
        return JsonSerializer.Deserialize<List<SaleRecord>>(json, _jsonOptions) ?? [];
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

    public SaleRecord Add(SaleRecord record)
    {
        _lock.Wait();
        try
        {
            var records = ReadAll();
            records.Add(record);
            WriteAll(records);
            return record;
        }
        finally { _lock.Release(); }
    }
}
