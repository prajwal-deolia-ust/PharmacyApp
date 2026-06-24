using PharmacyApp.Models;

namespace PharmacyApp.Services;

public interface ISaleService
{
    List<SaleRecord> GetAll();
    (SaleRecord? record, string? error) RecordSale(SaleRecord sale);
}
