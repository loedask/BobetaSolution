using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bobeta.Mobile.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event Action? StateChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected void RaiseStateChanged()
    {
        StateChanged?.Invoke();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    protected void SetLoading(bool value)
    {
        IsLoading = value;
        if (value) ErrorMessage = null;
        RaiseStateChanged();
    }

    protected void SetError(string? message)
    {
        ErrorMessage = message;
        RaiseStateChanged();
    }

    protected void ClearError()
    {
        ErrorMessage = null;
        RaiseStateChanged();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
