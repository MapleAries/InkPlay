using CommunityToolkit.Mvvm.ComponentModel;

namespace InkPlay.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "InkPlay";

    [ObservableProperty]
    private bool _isBackEnabled;
}
