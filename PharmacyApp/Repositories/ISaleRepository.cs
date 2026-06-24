using PharmacyApp.Models;

namespace PharmacyApp.Repositories;

public interface ISaleRepository
{
    List<SaleRecord> GetAll();
    SaleRecord Add(SaleRecord record);
}
