using Orders.Api;

namespace Orders.Tests;

public class OrderPricingTests
{
    [Fact]
    public void Small_orders_pay_full_price() =>
        Assert.Equal(45.00m, OrderPricing.CalculateTotal(quantity: 9, unitPrice: 5.00m));

    [Fact]
    public void Ten_or_more_items_get_bulk_discount() =>
        Assert.Equal(47.50m, OrderPricing.CalculateTotal(quantity: 10, unitPrice: 5.00m));

    [Fact]
    public void Total_is_rounded_to_cents() =>
        Assert.Equal(31.66m,OrderPricing.CalculateTotal(quantity: 10, unitPrice: 3.333m));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Quantity_must_be_positive(int quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderPricing.CalculateTotal(quantity, 5.00m));
}
