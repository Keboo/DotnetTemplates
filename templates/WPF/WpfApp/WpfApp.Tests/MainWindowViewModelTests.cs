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