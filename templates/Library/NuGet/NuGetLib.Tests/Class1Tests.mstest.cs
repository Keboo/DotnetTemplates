using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NuGetLib.Tests;

[TestClass]
public class Class1Tests
{
    [TestMethod]
    public void Method_WithPositiveValue_AddsOne()
    {
        //Arrange
        AutoMocker mocker = new();

        Class1 class1 = mocker.CreateInstance<Class1>();

        //Act
        int result = class1.Method(41);

        //Assert
        Assert.AreEqual(42, result);
    }
}
