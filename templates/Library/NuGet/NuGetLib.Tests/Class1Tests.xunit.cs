namespace NuGetLib.Tests;

public class Class1Tests
{
    [Fact]
    public void Method_WithPositiveValue_AddsOne()
    {
        //Arrange
        AutoMocker mocker = new();

        Class1 class1 = mocker.CreateInstance<Class1>();

        //Act
        int result = class1.Method(41);

        //Assert
        Assert.Equal(42, result);
    }
}