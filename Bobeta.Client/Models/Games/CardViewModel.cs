using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Bobeta.Client.Models.Games;

/// <summary>Playing card on the table UI (hand or board).</summary>
public class CardViewModel : INotifyPropertyChanged
{
    public string Suit { get; set; } = "";
    public string Rank { get; set; } = "";
    public string DisplayValue { get; set; } = "";
    public string CssClass { get; set; } = "";

    private bool _isPlayable = true;

    /// <summary>False when follow-suit requires a different card (server would reject the play).</summary>
    public bool IsPlayable
    {
        get => _isPlayable;
        set
        {
            if (_isPlayable == value)
                return;
            _isPlayable = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
