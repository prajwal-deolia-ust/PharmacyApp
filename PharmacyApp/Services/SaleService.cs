using PharmacyApp.Models;
using PharmacyApp.Repositories;

namespace PharmacyApp.Services;

public class SaleService : ISaleService
{
    private readonly ISaleRepository _repository;
    private readonly IMedicineService _medicineService;

    public SaleService(ISaleRepository repository, IMedicineService medicineService)
    {
        _repository = repository;
        _medicineService = medicineService;
    }

    public List<SaleRecord> GetAll() => _repository.GetAll();

    public (SaleRecord? record, string? error) RecordSale(SaleRecord sale)
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

        _repository.Add(sale);
        return (sale, null);
    }
}
