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

    private static void OnCardPropertyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((PlayingCardView)bindable).ApplyCard();

    public PlayingCardView()
    {
        InitializeComponent();
        ApplyCard();
    }

    public CardViewModel? Card
    {
        get => (CardViewModel?)GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    private void ApplyCard()
    {
        var c = Card;
        if (c == null)
        {
            FaceGrid.IsVisible = false;
            EmptyLabel.IsVisible = true;
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
    }
}
