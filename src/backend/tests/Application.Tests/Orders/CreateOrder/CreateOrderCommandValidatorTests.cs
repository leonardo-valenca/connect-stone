using Application.Orders.CreateOrder;

namespace Application.Tests.Orders.CreateOrder;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Valid_command_passes()
    {
        var result = _validator.Validate(new CreateOrderCommand("Jane Doe", "Coffee", 1500));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Coffee", 1500)]
    [InlineData("Jane Doe", "", 1500)]
    public void Blank_required_fields_fail(string customerName, string description, int amount)
    {
        var result = _validator.Validate(new CreateOrderCommand(customerName, description, amount));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_amount_fails(int amount)
    {
        var result = _validator.Validate(new CreateOrderCommand("Jane Doe", "Coffee", amount));

        Assert.False(result.IsValid);
    }
}
