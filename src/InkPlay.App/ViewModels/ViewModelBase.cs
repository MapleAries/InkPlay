using CommunityToolkit.Mvvm.ComponentModel;

namespace InkPlay.App.ViewModels;

public partial class ViewModelBase : ObservableObject
{
    public virtual void NavigatedTo(object? parameter) { }
    public virtual void NavigatedFrom() { }
}
