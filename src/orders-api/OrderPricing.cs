namespace Orders.Api;

/// <summary>Business rule covered by the unit tests: orders of 10 or more items get 5% off.</summary>
public static class OrderPricing
{
    public const int BulkQuantity = 10;
    public const decimal BulkDiscount = 0.05m;

    public static decimal CalculateTotal(int quantity, decimal unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        var total = quantity * unitPrice;
        if (quantity >= BulkQuantity)
            total -= total * BulkDiscount;

        return Math.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
