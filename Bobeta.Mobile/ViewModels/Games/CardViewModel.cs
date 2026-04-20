using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bobeta.Mobile.ViewModels.Games;

public class CardViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Suit { get; set; } = "";
    public string Rank { get; set; } = "";
    public string DisplayValue { get; set; } = "";

    private bool _isPlayable = true;
    public bool IsPlayable
    {
        get => _isPlayable;
        set
        {
            if (_isPlayable == value) return;
            _isPlayable = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
