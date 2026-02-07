namespace ADWebApplication.Tests;

using ADWebApplication.Services; 

public class CalculatorTests
{
    [Fact]
    public void Add_ReturnsCorrectSum()
    {
        var result = Calculator.Add(2, 3);
        Assert.Equal(5, result);
    }
}