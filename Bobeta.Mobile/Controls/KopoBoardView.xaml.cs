using Bobeta.Client.Models.Api;

namespace Bobeta.Mobile.Controls;

public partial class KopoBoardView : ContentView
{
    public static readonly BindableProperty BoardSizeProperty = BindableProperty.Create(
        nameof(BoardSize), typeof(int), typeof(KopoBoardView), 10, propertyChanged: OnBoardChanged);

    public static readonly BindableProperty PiecesProperty = BindableProperty.Create(
        nameof(Pieces), typeof(IList<KopoPieceDto>), typeof(KopoBoardView),
        propertyChanged: OnBoardChanged);

    public static readonly BindableProperty MyPlayerIdProperty = BindableProperty.Create(
        nameof(MyPlayerId), typeof(Guid), typeof(KopoBoardView), Guid.Empty, propertyChanged: OnBoardChanged);

    public static readonly BindableProperty SelectionPathProperty = BindableProperty.Create(
        nameof(SelectionPath), typeof(IList<KopoSquareDto>), typeof(KopoBoardView),
        propertyChanged: OnBoardChanged);

    public static readonly BindableProperty CanInteractProperty = BindableProperty.Create(
        nameof(CanInteract), typeof(bool), typeof(KopoBoardView), true, propertyChanged: OnBoardChanged);

    public event EventHandler<(int Row, int Col)>? CellTapped;

    private readonly Dictionary<(int Row, int Col), Button> _cells = new();

    public KopoBoardView()
    {
        InitializeComponent();
        EnsureGrid();
    }

    public int BoardSize
    {
        get => (int)GetValue(BoardSizeProperty);
        set => SetValue(BoardSizeProperty, value);
    }

    public IList<KopoPieceDto> Pieces
    {
        get => (IList<KopoPieceDto>)GetValue(PiecesProperty);
        set => SetValue(PiecesProperty, value);
    }

    public Guid MyPlayerId
    {
        get => (Guid)GetValue(MyPlayerIdProperty);
        set => SetValue(MyPlayerIdProperty, value);
    }

    public IList<KopoSquareDto> SelectionPath
    {
        get => (IList<KopoSquareDto>)GetValue(SelectionPathProperty);
        set => SetValue(SelectionPathProperty, value);
    }

    public bool CanInteract
    {
        get => (bool)GetValue(CanInteractProperty);
        set => SetValue(CanInteractProperty, value);
    }

    private static void OnBoardChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is KopoBoardView view)
            view.RefreshCells();
    }

    private void EnsureGrid()
    {
        var size = Math.Clamp(BoardSize, 8, 12);
        if (BoardGrid.RowDefinitions.Count == size && BoardGrid.ColumnDefinitions.Count == size)
            return;

        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        BoardGrid.Children.Clear();
        _cells.Clear();

        for (var i = 0; i < size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var r = 0; r < size; r++)
        for (var c = 0; c < size; c++)
        {
            var row = r;
            var col = c;
            var btn = new Button
            {
                Padding = 0,
                FontSize = 11,
                CornerRadius = 2,
                BorderWidth = 0
            };
            btn.Clicked += (_, _) =>
            {
                if (CanInteract)
                    CellTapped?.Invoke(this, (row, col));
            };
            Grid.SetRow(btn, row);
            Grid.SetColumn(btn, col);
            BoardGrid.Children.Add(btn);
            _cells[(row, col)] = btn;
        }
    }

    private void RefreshCells()
    {
        EnsureGrid();
        var size = Math.Clamp(BoardSize, 8, 12);
        var pieces = Pieces ?? Array.Empty<KopoPieceDto>();
        var path = SelectionPath ?? Array.Empty<KopoSquareDto>();

        for (var r = 0; r < size; r++)
        for (var c = 0; c < size; c++)
        {
            if (!_cells.TryGetValue((r, c), out var btn))
                continue;

            var dark = (r + c) % 2 == 1;
            var selected = path.Any(p => p.Row == r && p.Col == c);
            var piece = pieces.FirstOrDefault(p => p.Row == r && p.Col == c);

            btn.BackgroundColor = dark
                ? Color.FromArgb(selected ? "#5c4a1a" : "#3d2914")
                : Color.FromArgb("#1e2430");
            btn.IsEnabled = CanInteract;

            if (piece == null)
            {
                btn.Text = "";
                btn.TextColor = Colors.Transparent;
            }
            else
            {
                var mine = piece.OwnerId == MyPlayerId;
                btn.Text = piece.IsKing ? "♔" : "●";
                btn.TextColor = mine ? Color.FromArgb("#eab308") : Color.FromArgb("#e2e8f0");
            }
        }
    }
}
