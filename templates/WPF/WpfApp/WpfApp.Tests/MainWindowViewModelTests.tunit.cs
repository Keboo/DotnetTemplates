namespace WpfApp.Tests;

//This attribute generates tests for MainWindowViewModel that
//asserts all constructor arguments are checked for null
[ConstructorTests(typeof(MainWindowViewModel))]
public partial class MainWindowViewModelTests
{
    [Test]
    public async Task IncrementCounterCommand_Execute_IncrementsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        int initialCount = viewModel.Count;

        //Act
        viewModel.IncrementCountCommand.Execute(null);

        //Assert
        await Assert.That(viewModel.Count).IsEqualTo(initialCount + 1);
    }

    [Test]
    [Arguments(0, true)]
    [Arguments(1, true)]
    [Arguments(2, true)]
    [Arguments(3, true)]
    [Arguments(4, true)]
    [Arguments(5, false)]
    [Arguments(6, false)]
    public async Task IncrementCounterCommand_CanExecute_IndicatesIfCountIsLessThanFive(int count, bool expected)
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        viewModel.Count = count;

        //Act
        bool canExecute = viewModel.IncrementCountCommand.CanExecute(null);

        //Assert
        await Assert.That(canExecute).IsEqualTo(expected);
    }

    [Test]
    public async Task ClearCounterCommand_Execute_ClearsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        viewModel.Count = 42;

        //Act
        viewModel.ClearCountCommand.Execute(null);

        //Assert
        await Assert.That(viewModel.Count).IsEqualTo(0);
    }
}
