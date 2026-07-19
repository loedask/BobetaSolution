using Bobeta.Client.Models.Api;

namespace Bobeta.Mobile.Controls;

public partial class YoteBoardView : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(YoteStateDto), typeof(YoteBoardView), null,
        propertyChanged: (bindable, _, _) => ((YoteBoardView)bindable).Render());

    public static readonly BindableProperty CanInteractProperty = BindableProperty.Create(
        nameof(CanInteract), typeof(bool), typeof(YoteBoardView), false,
        propertyChanged: (bindable, _, _) => ((YoteBoardView)bindable).Render());

    private int? _selectedFrom;
    private bool _removeMode;
    private int? _pendingFrom;
    private int? _pendingTo;

    public YoteBoardView()
    {
        InitializeComponent();
        Render();
    }

    public YoteStateDto? State
    {
        get => (YoteStateDto?)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool CanInteract
    {
        get => (bool)GetValue(CanInteractProperty);
        set => SetValue(CanInteractProperty, value);
    }

    public event EventHandler<(int? FromCell, int ToCell, int? ExtraRemoveCell)>? MoveRequested;

    private void Render()
    {
        if (BoardGrid == null)
            return;

        var state = State;
        var rows = state?.Rows > 0 ? state.Rows : 5;
        var cols = state?.Cols > 0 ? state.Cols : 6;
        var cellCount = rows * cols;
        var occupancy = state?.Occupancy ?? Enumerable.Repeat(0, cellCount).ToList();
        var legalPlaces = new HashSet<int>(state?.LegalPlaceCells ?? []);
        var legalSlides = state?.LegalSlides ?? [];
        var legalCaptures = state?.LegalCaptures ?? [];

        if (_selectedFrom.HasValue
            && legalSlides.All(s => s.From != _selectedFrom.Value)
            && legalCaptures.All(c => c.From != _selectedFrom.Value))
            _selectedFrom = null;

        StatusLabel.Text = _removeMode
            ? "Tap an opponent piece to remove"
            : _selectedFrom.HasValue
                ? "Choose destination"
                : $"In hand: {state?.MyInHand ?? 0} · Opponent: {state?.OpponentInHand ?? 0}";

        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        for (var r = 0; r < rows; r++)
            BoardGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        for (var c = 0; c < cols; c++)
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        for (var cell = 0; cell < cellCount; cell++)
        {
            var row = cell / cols;
            var col = cell % cols;
            var occ = occupancy.Count > cell ? occupancy[cell] : 0;
            var isMine = occ == 1;
            var isOpponent = occ == 2;
            var isEmpty = occ == 0;
            var isSelected = _selectedFrom == cell;
            var isTarget = _selectedFrom.HasValue
                && (legalSlides.Any(s => s.From == _selectedFrom.Value && s.To == cell)
                    || legalCaptures.Any(c => c.From == _selectedFrom.Value && c.To == cell));
            var isRemoveTarget = _removeMode && isOpponent;
            var canSelect = ! _removeMode && isMine
                && (legalSlides.Any(s => s.From == cell) || legalCaptures.Any(c => c.From == cell));
            var canPlace = !_removeMode && _selectedFrom == null && legalPlaces.Contains(cell);
            var enabled = CanInteract && state?.CanAct == true
                && (isRemoveTarget || isTarget || canSelect || canPlace || isSelected);

            var button = new Button
            {
                Text = isEmpty ? "" : "●",
                FontSize = isEmpty ? 14 : 18,
                CornerRadius = 8,
                HeightRequest = 40,
                Padding = 0,
                BackgroundColor = isMine
                    ? Color.FromArgb("#C9A227")
                    : isOpponent
                        ? Color.FromArgb("#64748b")
                        : Color.FromArgb("#1e293b"),
                TextColor = isEmpty ? Colors.Transparent : Color.FromArgb("#0f172a"),
                BorderColor = isSelected
                    ? Color.FromArgb("#2dd48e")
                    : isTarget || isRemoveTarget
                        ? Color.FromArgb("#38bdf8")
                        : Color.FromArgb("#334155"),
                BorderWidth = isSelected || isTarget || isRemoveTarget ? 2 : 1,
                IsEnabled = enabled || isSelected,
                Opacity = isOpponent && !_removeMode && !isSelected ? 0.9 : 1
            };

            var capturedCell = cell;
            button.Clicked += (_, _) => OnCellClicked(capturedCell, legalPlaces, legalSlides, legalCaptures, occupancy);
            BoardGrid.Add(button, col, row);
        }
    }

    private void OnCellClicked(
        int cell,
        HashSet<int> legalPlaces,
        IReadOnlyList<YoteEdgeDto> legalSlides,
        IReadOnlyList<YoteCaptureDto> legalCaptures,
        IReadOnlyList<int> occupancy)
    {
        if (!CanInteract || State?.CanAct != true)
            return;

        if (_removeMode && _pendingFrom is int capFrom && _pendingTo is int capTo)
        {
            if (occupancy.Count <= cell || occupancy[cell] != 2)
                return;

            _removeMode = false;
            _pendingFrom = null;
            _pendingTo = null;
            _selectedFrom = null;
            MoveRequested?.Invoke(this, (capFrom, capTo, cell));
            return;
        }

        if (_selectedFrom == cell)
        {
            _selectedFrom = null;
            Render();
            return;
        }

        if (_selectedFrom is { } from)
        {
            if (legalSlides.Any(s => s.From == from && s.To == cell))
            {
                _selectedFrom = null;
                MoveRequested?.Invoke(this, (from, cell, null));
                return;
            }

            var capture = legalCaptures.FirstOrDefault(c => c.From == from && c.To == cell);
            if (capture != null)
            {
                var opponentCount = occupancy.Count(x => x == 2);
                if (opponentCount > 1)
                {
                    _removeMode = true;
                    _pendingFrom = from;
                    _pendingTo = cell;
                    _selectedFrom = null;
                    Render();
                    return;
                }

                _selectedFrom = null;
                MoveRequested?.Invoke(this, (from, cell, null));
            }

            return;
        }

        if (legalPlaces.Contains(cell))
        {
            MoveRequested?.Invoke(this, (null, cell, null));
            return;
        }

        if (occupancy.Count > cell && occupancy[cell] == 1
            && (legalSlides.Any(s => s.From == cell) || legalCaptures.Any(c => c.From == cell)))
        {
            _selectedFrom = cell;
            Render();
        }
    }
}
