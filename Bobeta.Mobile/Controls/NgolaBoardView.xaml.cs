namespace Bobeta.Mobile.Controls;

public partial class NgolaBoardView : ContentView
{
    public static readonly BindableProperty MyPitsProperty = BindableProperty.Create(
        nameof(MyPits), typeof(IReadOnlyList<int>), typeof(NgolaBoardView), Array.Empty<int>(),
        propertyChanged: (bindable, _, _) => ((NgolaBoardView)bindable).Render());

    public static readonly BindableProperty OpponentPitsProperty = BindableProperty.Create(
        nameof(OpponentPits), typeof(IReadOnlyList<int>), typeof(NgolaBoardView), Array.Empty<int>(),
        propertyChanged: (bindable, _, _) => ((NgolaBoardView)bindable).Render());

    public static readonly BindableProperty MyScoreProperty = BindableProperty.Create(
        nameof(MyScore), typeof(int), typeof(NgolaBoardView), 0,
        propertyChanged: (bindable, _, _) => ((NgolaBoardView)bindable).Render());

    public static readonly BindableProperty OpponentScoreProperty = BindableProperty.Create(
        nameof(OpponentScore), typeof(int), typeof(NgolaBoardView), 0,
        propertyChanged: (bindable, _, _) => ((NgolaBoardView)bindable).Render());

    public static readonly BindableProperty CanInteractProperty = BindableProperty.Create(
        nameof(CanInteract), typeof(bool), typeof(NgolaBoardView), false,
        propertyChanged: (bindable, _, _) => ((NgolaBoardView)bindable).Render());

    public NgolaBoardView()
    {
        InitializeComponent();
        Render();
    }

    public IReadOnlyList<int> MyPits
    {
        get => (IReadOnlyList<int>)GetValue(MyPitsProperty);
        set => SetValue(MyPitsProperty, value);
    }

    public IReadOnlyList<int> OpponentPits
    {
        get => (IReadOnlyList<int>)GetValue(OpponentPitsProperty);
        set => SetValue(OpponentPitsProperty, value);
    }

    public int MyScore
    {
        get => (int)GetValue(MyScoreProperty);
        set => SetValue(MyScoreProperty, value);
    }

    public int OpponentScore
    {
        get => (int)GetValue(OpponentScoreProperty);
        set => SetValue(OpponentScoreProperty, value);
    }

    public bool CanInteract
    {
        get => (bool)GetValue(CanInteractProperty);
        set => SetValue(CanInteractProperty, value);
    }

    public event EventHandler<int>? PitTapped;

    private void Render()
    {
        if (BoardGrid == null)
            return;
        BoardGrid.Children.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        var count = Math.Max(MyPits.Count, OpponentPits.Count);
        for (var i = 0; i < count; i++)
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        OpponentScoreLabel.Text = $"Opponent: {OpponentScore}";
        MyScoreLabel.Text = $"You: {MyScore}";
        for (var i = 0; i < OpponentPits.Count; i++)
            AddPit(OpponentPits[i], row: 0, col: i, localIndex: null);
        for (var i = 0; i < MyPits.Count; i++)
            AddPit(MyPits[i], row: 1, col: i, localIndex: i);
    }

    private void AddPit(int seeds, int row, int col, int? localIndex)
    {
        var enabled = localIndex.HasValue && CanInteract && seeds >= 2;
        var button = new Button
        {
            Text = seeds.ToString(),
            CornerRadius = 24,
            HeightRequest = 48,
            MinimumWidthRequest = 36,
            Padding = 0,
            BackgroundColor = Color.FromArgb("#FDE68A"),
            TextColor = Color.FromArgb("#451A03"),
            FontAttributes = FontAttributes.Bold,
            IsEnabled = enabled,
            Opacity = enabled || !localIndex.HasValue ? 1 : 0.7
        };
        if (localIndex is { } pit)
            button.Clicked += (_, _) => PitTapped?.Invoke(this, pit);
        BoardGrid.Add(button, col, row);
    }
}
