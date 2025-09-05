using AzKview.Core.Services;

namespace AzKview.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel() : this(new TimeService()) { }

    public MainWindowViewModel(ITimeService timeService)
    {
        Greeting = $"Welcome to Avalonia! UTC: {timeService.UtcNow:O}";
    }

    public string Greeting { get; }
}
