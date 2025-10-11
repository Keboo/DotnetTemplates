using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace NuGetLib.Tests;

public class Class1Tests
{
    [Test]
    public async Task Method_WithPositiveValue_AddsOne()
    {
        //Arrange
        AutoMocker mocker = new();

        Class1 class1 = mocker.CreateInstance<Class1>();

        //Act
        int result = class1.Method(41);

        //Assert
        await Assert.That(result).IsEqualTo(42);
    }
}
