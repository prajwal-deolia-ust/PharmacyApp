using PharmacyApp.Models;

namespace PharmacyApp.UnitTests.Models;

[TestClass]
public class SaleRecordTests
{
    // TotalAmount = QuantitySold * PricePerUnit

    [TestMethod]
    public void TotalAmount_PositiveQuantityAndPositivePrice_ReturnsProduct()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 5,
            PricePerUnit = 10.00m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(50.00m, result);
    }

    [TestMethod]
    public void TotalAmount_ZeroQuantity_ReturnsZero()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 0,
            PricePerUnit = 25.99m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void TotalAmount_ZeroPrice_ReturnsZero()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 10,
            PricePerUnit = 0m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void TotalAmount_DefaultInstance_ReturnsZero()
    {
        // Arrange
        var saleRecord = new SaleRecord(); // QuantitySold = 0 (default int), PricePerUnit = 0 (default decimal)

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void TotalAmount_DecimalPricePerUnit_ReturnsCorrectProduct()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 3,
            PricePerUnit = 1.99m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(5.97m, result);
    }

    [TestMethod]
    public void TotalAmount_LargeQuantityAndLargePrice_ReturnsCorrectProduct()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 1000,
            PricePerUnit = 999.99m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(999990.00m, result);
    }

    [TestMethod]
    public void TotalAmount_QuantityOfOne_ReturnsPricePerUnit()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 1,
            PricePerUnit = 42.50m,
        };

        // Act
        var result = saleRecord.TotalAmount;

        // Assert
        Assert.AreEqual(42.50m, result);
    }

    [TestMethod]
    public void TotalAmount_RecomputesWhenQuantitySoldChanges()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 2,
            PricePerUnit = 10.00m,
        };

        // Act & Assert – initial value
        Assert.AreEqual(20.00m, saleRecord.TotalAmount);

        // Update quantity
        saleRecord.QuantitySold = 5;

        // Assert – recomputed value
        Assert.AreEqual(50.00m, saleRecord.TotalAmount);
    }

    [TestMethod]
    public void TotalAmount_RecomputesWhenPricePerUnitChanges()
    {
        // Arrange
        var saleRecord = new SaleRecord
        {
            QuantitySold = 4,
            PricePerUnit = 5.00m,
        };

        // Act & Assert – initial value
        Assert.AreEqual(20.00m, saleRecord.TotalAmount);

        // Update price
        saleRecord.PricePerUnit = 15.00m;

        // Assert – recomputed value
        Assert.AreEqual(60.00m, saleRecord.TotalAmount);
    }
}
