namespace Bobeta.Mobile.Controls;

public partial class DominoBoardView : ContentView
{
    public static readonly BindableProperty MyHandProperty = BindableProperty.Create(
        nameof(MyHand), typeof(IReadOnlyList<string>), typeof(DominoBoardView), Array.Empty<string>(),
        propertyChanged: OnChanged);
    public static readonly BindableProperty OpponentHandCountProperty = BindableProperty.Create(
        nameof(OpponentHandCount), typeof(int), typeof(DominoBoardView), 0, propertyChanged: OnChanged);
    public static readonly BindableProperty BoneyardCountProperty = BindableProperty.Create(
        nameof(BoneyardCount), typeof(int), typeof(DominoBoardView), 0, propertyChanged: OnChanged);
    public static readonly BindableProperty ChainProperty = BindableProperty.Create(
        nameof(Chain), typeof(IReadOnlyList<string>), typeof(DominoBoardView), Array.Empty<string>(),
        propertyChanged: OnChanged);
    public static readonly BindableProperty LeftEndProperty = BindableProperty.Create(
        nameof(LeftEnd), typeof(int?), typeof(DominoBoardView), null, propertyChanged: OnChanged);
    public static readonly BindableProperty RightEndProperty = BindableProperty.Create(
        nameof(RightEnd), typeof(int?), typeof(DominoBoardView), null, propertyChanged: OnChanged);
    public static readonly BindableProperty IsOpeningProperty = BindableProperty.Create(
        nameof(IsOpening), typeof(bool), typeof(DominoBoardView), false, propertyChanged: OnChanged);
    public static readonly BindableProperty OpeningTileProperty = BindableProperty.Create(
        nameof(OpeningTile), typeof(string), typeof(DominoBoardView), null, propertyChanged: OnChanged);
    public static readonly BindableProperty MustDrawProperty = BindableProperty.Create(
        nameof(MustDraw), typeof(bool), typeof(DominoBoardView), false, propertyChanged: OnChanged);
    public static readonly BindableProperty MustPassProperty = BindableProperty.Create(
        nameof(MustPass), typeof(bool), typeof(DominoBoardView), false, propertyChanged: OnChanged);
    public static readonly BindableProperty CanInteractProperty = BindableProperty.Create(
        nameof(CanInteract), typeof(bool), typeof(DominoBoardView), false, propertyChanged: OnChanged);

    private string? _selectedTile;

    public DominoBoardView()
    {
        InitializeComponent();
        Render();
    }

    public IReadOnlyList<string> MyHand
    {
        get => (IReadOnlyList<string>)GetValue(MyHandProperty);
        set => SetValue(MyHandProperty, value);
    }
    public int OpponentHandCount
    {
        get => (int)GetValue(OpponentHandCountProperty);
        set => SetValue(OpponentHandCountProperty, value);
    }
    public int BoneyardCount
    {
        get => (int)GetValue(BoneyardCountProperty);
        set => SetValue(BoneyardCountProperty, value);
    }
    public IReadOnlyList<string> Chain
    {
        get => (IReadOnlyList<string>)GetValue(ChainProperty);
        set => SetValue(ChainProperty, value);
    }
    public int? LeftEnd
    {
        get => (int?)GetValue(LeftEndProperty);
        set => SetValue(LeftEndProperty, value);
    }
    public int? RightEnd
    {
        get => (int?)GetValue(RightEndProperty);
        set => SetValue(RightEndProperty, value);
    }
    public bool IsOpening
    {
        get => (bool)GetValue(IsOpeningProperty);
        set => SetValue(IsOpeningProperty, value);
    }
    public string? OpeningTile
    {
        get => (string?)GetValue(OpeningTileProperty);
        set => SetValue(OpeningTileProperty, value);
    }
    public bool MustDraw
    {
        get => (bool)GetValue(MustDrawProperty);
        set => SetValue(MustDrawProperty, value);
    }
    public bool MustPass
    {
        get => (bool)GetValue(MustPassProperty);
        set => SetValue(MustPassProperty, value);
    }
    public bool CanInteract
    {
        get => (bool)GetValue(CanInteractProperty);
        set => SetValue(CanInteractProperty, value);
    }

    public event EventHandler<(string Action, int? High, int? Low, string? End)>? ActionRequested;

    private static void OnChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DominoBoardView)bindable).Render();

    private void Render()
    {
        if (Root == null)
            return;
        if (_selectedTile != null && !MyHand.Contains(_selectedTile))
            _selectedTile = null;

        Root.Children.Clear();
        Root.Children.Add(new Label
        {
            Text = $"Opponent: {OpponentHandCount}   Boneyard: {BoneyardCount}",
            TextColor = Colors.LightGray,
            FontAttributes = FontAttributes.Bold,
            FontSize = 12
        });

        var chainRow = new HorizontalStackLayout { Spacing = 4 };
        if (Chain.Count == 0)
        {
            chainRow.Children.Add(new Label
            {
                Text = IsOpening && !string.IsNullOrEmpty(OpeningTile)
                    ? $"Play opening tile {OpeningTile}"
                    : "Waiting for opening tile…",
                TextColor = Colors.Gray,
                FontSize = 12
            });
        }
        else
        {
            foreach (var tile in Chain)
                chainRow.Children.Add(TileLabel(tile, selected: false, enabled: false));
        }

        Root.Children.Add(new ScrollView
        {
            Orientation = ScrollOrientation.Horizontal,
            Content = chainRow,
            HeightRequest = 48
        });

        if (!IsOpening && CanInteract && !MustDraw && !MustPass && !string.IsNullOrEmpty(_selectedTile))
        {
            var ends = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(), new() }, ColumnSpacing = 8 };
            var left = EndButton($"Left ({LeftEnd})", "left");
            var right = EndButton($"Right ({RightEnd})", "right");
            ends.Add(left, 0);
            ends.Add(right, 1);
            Root.Children.Add(ends);
        }

        var hand = new FlexLayout
        {
            Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
            JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Center
        };
        foreach (var tile in MyHand)
        {
            var playable = CanInteract && !MustDraw && !MustPass && IsPlayable(tile);
            var btn = TileButton(tile, _selectedTile == tile, playable);
            var captured = tile;
            btn.Clicked += (_, _) => _ = OnTileClickedAsync(captured);
            hand.Children.Add(btn);
        }
        Root.Children.Add(hand);

        var actions = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new(), new() }, ColumnSpacing = 8 };
        var draw = new Button
        {
            Text = "Draw",
            IsEnabled = CanInteract && MustDraw,
            CornerRadius = 10,
            BackgroundColor = Color.FromArgb("#1E293B"),
            TextColor = Colors.White
        };
        draw.Clicked += (_, _) => ActionRequested?.Invoke(this, ("draw", null, null, null));
        var pass = new Button
        {
            Text = "Pass",
            IsEnabled = CanInteract && MustPass,
            CornerRadius = 10,
            BackgroundColor = Color.FromArgb("#1E293B"),
            TextColor = Colors.White
        };
        pass.Clicked += (_, _) => ActionRequested?.Invoke(this, ("pass", null, null, null));
        actions.Add(draw, 0);
        actions.Add(pass, 1);
        Root.Children.Add(actions);
    }

    private Button EndButton(string text, string end)
    {
        var btn = new Button
        {
            Text = text,
            CornerRadius = 10,
            BackgroundColor = Color.FromArgb("#EAB308"),
            TextColor = Color.FromArgb("#12151F"),
            FontAttributes = FontAttributes.Bold
        };
        btn.Clicked += (_, _) =>
        {
            if (string.IsNullOrEmpty(_selectedTile))
                return;
            var (h, l) = Parse(_selectedTile);
            ActionRequested?.Invoke(this, ("play", h, l, end));
            _selectedTile = null;
        };
        return btn;
    }

    private Task OnTileClickedAsync(string tile)
    {
        if (IsOpening)
        {
            var (h, l) = Parse(tile);
            ActionRequested?.Invoke(this, ("play", h, l, null));
            return Task.CompletedTask;
        }

        var leftOk = Matches(tile, LeftEnd);
        var rightOk = Matches(tile, RightEnd);
        if (leftOk && !rightOk)
        {
            var (h, l) = Parse(tile);
            ActionRequested?.Invoke(this, ("play", h, l, "left"));
            return Task.CompletedTask;
        }

        if (rightOk && !leftOk)
        {
            var (h, l) = Parse(tile);
            ActionRequested?.Invoke(this, ("play", h, l, "right"));
            return Task.CompletedTask;
        }

        _selectedTile = _selectedTile == tile ? null : tile;
        Render();
        return Task.CompletedTask;
    }

    private bool IsPlayable(string tile) =>
        IsOpening ? tile == OpeningTile : Matches(tile, LeftEnd) || Matches(tile, RightEnd);

    private static bool Matches(string tile, int? end)
    {
        if (end is null) return false;
        var (h, l) = Parse(tile);
        return h == end || l == end;
    }

    private static (int High, int Low) Parse(string tile)
    {
        var parts = tile.Split('-');
        var a = int.Parse(parts[0]);
        var b = int.Parse(parts[1]);
        return a >= b ? (a, b) : (b, a);
    }

    private static string Format(string tile)
    {
        var (h, l) = Parse(tile);
        return $"{h}|{l}";
    }

    private static Label TileLabel(string tile, bool selected, bool enabled) =>
        new()
        {
            Text = Format(tile),
            Padding = new Thickness(8, 6),
            BackgroundColor = selected ? Color.FromArgb("#EAB308") : Color.FromArgb("#E2E8F0"),
            TextColor = Color.FromArgb("#0F172A"),
            FontAttributes = FontAttributes.Bold,
            Opacity = enabled || selected ? 1 : 0.85
        };

    private static Button TileButton(string tile, bool selected, bool enabled) =>
        new()
        {
            Text = Format(tile),
            Padding = new Thickness(8, 6),
            Margin = new Thickness(2),
            CornerRadius = 8,
            BackgroundColor = selected ? Color.FromArgb("#EAB308") : Color.FromArgb("#E2E8F0"),
            TextColor = Color.FromArgb("#0F172A"),
            FontAttributes = FontAttributes.Bold,
            IsEnabled = enabled,
            Opacity = enabled ? 1 : 0.7
        };
}
