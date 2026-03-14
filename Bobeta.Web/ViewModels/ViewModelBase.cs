namespace Bobeta.Web.ViewModels;

/// <summary>Base for view models that need to notify the UI of state changes.</summary>
public abstract class ViewModelBase
{
    public event Action? StateChanged;

    public bool IsLoading { get; protected set; }
    public string? ErrorMessage { get; protected set; }

    protected void RaiseStateChanged() => StateChanged?.Invoke();

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
}
