namespace WpfApp.Tests;

//This attribute generates tests for MainWindowViewModel that
//asserts all constructor arguments are checked for null
[ConstructorTests(typeof(MainWindowViewModel))]
public partial class MainWindowViewModelTests
{
    [Fact]
    public void IncrementCounterCommand_Execute_IncrementsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        int initialCount = viewModel.Count;

        //Act
        viewModel.IncrementCountCommand.Execute(null);

        //Assert
        Assert.Equal(initialCount + 1, viewModel.Count);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    public void IncrementCounterCommand_CanExecute_IndicatesIfCountIsLessThanFive(int count, bool expected)
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();

        viewModel.Count = count;

        //Act
        bool canExecute = viewModel.IncrementCountCommand.CanExecute(null);

        //Assert
        Assert.Equal(expected, canExecute);
    }

    [Fact]
    public void ClearCounterCommand_Execute_ClearsCount()
    {
        //Arrange
        AutoMocker mocker = new();

        MainWindowViewModel viewModel = mocker.CreateInstance<MainWindowViewModel>();
        viewModel.Count = 42;

        //Act
        viewModel.ClearCountCommand.Execute(null);

        //Assert
        Assert.Equal(0, viewModel.Count);
    }
}