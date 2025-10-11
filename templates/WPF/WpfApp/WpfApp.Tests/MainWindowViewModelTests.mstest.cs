namespace WpfApp.Tests;

//This attribute generates tests for MainWindowViewModel that
//asserts all constructor arguments are checked for null
[ConstructorTests(typeof(MainWindowViewModel))]
[TestClass]
public partial class MainWindowViewModelTests
{
    [TestMethod]
    public void IncrementCounterCommand_Execute_IncrementsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        int initialCount = viewModel.Count;

        //Act
        viewModel.IncrementCountCommand.Execute(null);

        //Assert
        Assert.AreEqual(initialCount + 1, viewModel.Count);
    }

    [DataTestMethod]
    [DataRow(0, true)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(4, true)]
    [DataRow(5, false)]
    [DataRow(6, false)]
    public void IncrementCounterCommand_CanExecute_IndicatesIfCountIsLessThanFive(int count, bool expected)
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        viewModel.Count = count;

        //Act
        bool canExecute = viewModel.IncrementCountCommand.CanExecute(null);

        //Assert
        Assert.AreEqual(expected, canExecute);
    }

    [TestMethod]
    public void ClearCounterCommand_Execute_ClearsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        viewModel.Count = 42;

        //Act
        viewModel.ClearCountCommand.Execute(null);

        //Assert
        Assert.AreEqual(0, viewModel.Count);
    }
}
