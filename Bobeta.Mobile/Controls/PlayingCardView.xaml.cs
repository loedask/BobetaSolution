using System.ComponentModel;
using Bobeta.Client.Presentation;
using Bobeta.Mobile.ViewModels.Games;

namespace Bobeta.Mobile.Controls;

public partial class PlayingCardView : ContentView
{
    public static readonly BindableProperty CardProperty = BindableProperty.Create(
        nameof(Card),
        typeof(CardViewModel),
        typeof(PlayingCardView),
        default(CardViewModel?),
        propertyChanged: OnCardPropertyChanged);

    private CardViewModel? _subscribedCard;

    private static void OnCardPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (PlayingCardView)bindable;
        view.DetachCardSubscription();
        view._subscribedCard = view.Card;
        if (view._subscribedCard != null)
            view._subscribedCard.PropertyChanged += view.OnBoundCardPropertyChanged;
        view.ApplyCard();
    }

    public PlayingCardView()
    {
        InitializeComponent();
        Unloaded += (_, _) => DetachCardSubscription();
        ApplyCard();
    }

    public CardViewModel? Card
    {
        get => (CardViewModel?)GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    private void DetachCardSubscription()
    {
        if (_subscribedCard != null)
        {
            _subscribedCard.PropertyChanged -= OnBoundCardPropertyChanged;
            _subscribedCard = null;
        }
    }

    private void OnBoundCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CardViewModel.IsPlayable))
            return;
        MainThread.BeginInvokeOnMainThread(ApplyPlayableOpacity);
    }

    private void ApplyPlayableOpacity()
    {
        if (Card == null) return;
        Chrome.Opacity = Card.IsPlayable ? 1 : 0.38;
    }

    private void ApplyCard()
    {
        var c = Card;
        if (c == null)
        {
            FaceGrid.IsVisible = false;
            EmptyLabel.IsVisible = true;
            Chrome.Opacity = 1;
            return;
        }

        var (rank, suit, red) = PlayingCardFormat.Resolve(c.Suit, c.Rank);
        var ink = Color.FromArgb(red ? "#b91c1c" : "#0f172a");

        CornerRankTop.Text = rank;
        CornerSuitTop.Text = suit;
        CenterSuit.Text = suit;
        CornerRankBottom.Text = rank;
        CornerSuitBottom.Text = suit;

        CornerRankTop.TextColor = ink;
        CornerSuitTop.TextColor = ink;
        CenterSuit.TextColor = ink;
        CornerRankBottom.TextColor = ink;
        CornerSuitBottom.TextColor = ink;

        FaceGrid.IsVisible = true;
        EmptyLabel.IsVisible = false;
        ApplyPlayableOpacity();
    }
}
