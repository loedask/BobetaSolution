using Bobeta.Client.Models.Api;

namespace Bobeta.Mobile.Controls;

public partial class NzengueBoardView : ContentView
{
    public static readonly BindableProperty StateProperty = BindableProperty.Create(
        nameof(State), typeof(NzengueStateDto), typeof(NzengueBoardView), null,
        propertyChanged: (bindable, _, _) => ((NzengueBoardView)bindable).Render());

    public static readonly BindableProperty CanInteractProperty = BindableProperty.Create(
        nameof(CanInteract), typeof(bool), typeof(NzengueBoardView), false,
        propertyChanged: (bindable, _, _) => ((NzengueBoardView)bindable).Render());

    private int? _selectedFrom;

    public NzengueBoardView()
    {
        InitializeComponent();
        Render();
    }

    public NzengueStateDto? State
    {
        get => (NzengueStateDto?)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool CanInteract
    {
        get => (bool)GetValue(CanInteractProperty);
        set => SetValue(CanInteractProperty, value);
    }

    public event EventHandler<(int? FromPoint, int ToPoint)>? MoveRequested;

    private void Render()
    {
        if (BoardGrid == null)
            return;

        var state = State;
        var occupancy = state?.Occupancy ?? Enumerable.Repeat(0, 9).ToList();
        var phase = state?.Phase ?? NzengueRulesPhasePlace;
        var legalPlaces = new HashSet<int>(state?.LegalPlacePoints ?? []);
        var legalMoves = state?.LegalMoves ?? [];
        var isPlace = string.Equals(phase, NzengueRulesPhasePlace, StringComparison.OrdinalIgnoreCase);

        if (_selectedFrom.HasValue && !IsLegalFrom(_selectedFrom.Value, legalMoves))
            _selectedFrom = null;

        PhaseLabel.Text = isPlace
            ? $"Place ({state?.MyPiecesToPlace ?? 0} left)"
            : _selectedFrom.HasValue
                ? "Choose destination"
                : "Select a stone to move";

        BoardGrid.Children.Clear();
        for (var point = 0; point < 9; point++)
        {
            var row = point / 3;
            var col = point % 3;
            var cell = occupancy.Count > point ? occupancy[point] : 0;
            var isMine = cell == 1;
            var isOpponent = cell == 2;
            var isEmpty = cell == 0;
            var isSelected = _selectedFrom == point;
            var isTarget = isPlace
                ? isEmpty && legalPlaces.Contains(point)
                : _selectedFrom.HasValue
                    && legalMoves.Any(m => m.From == _selectedFrom.Value && m.To == point);
            var canSelect = !isPlace && isMine && legalMoves.Any(m => m.From == point);
            var enabled = CanInteract && state?.CanAct == true && (isTarget || canSelect || (isPlace && isTarget));

            var button = new Button
            {
                Text = isMine ? "●" : isOpponent ? "●" : "○",
                FontSize = isEmpty ? 18 : 22,
                CornerRadius = 24,
                HeightRequest = 52,
                WidthRequest = 52,
                Padding = 0,
                BackgroundColor = isMine
                    ? Color.FromArgb("#C9A227")
                    : isOpponent
                        ? Color.FromArgb("#64748b")
                        : Color.FromArgb("#1e293b"),
                TextColor = isEmpty ? Color.FromArgb("#64748b") : Color.FromArgb("#0f172a"),
                BorderColor = isSelected
                    ? Color.FromArgb("#2dd48e")
                    : isTarget
                        ? Color.FromArgb("#38bdf8")
                        : Color.FromArgb("#334155"),
                BorderWidth = isSelected || isTarget ? 2 : 1,
                IsEnabled = enabled || isSelected,
                Opacity = isOpponent || (!enabled && !isSelected && !(isPlace && isEmpty)) ? 0.85 : 1
            };

            var capturedPoint = point;
            button.Clicked += (_, _) => OnPointClicked(capturedPoint, isPlace, legalPlaces, legalMoves);
            BoardGrid.Add(button, col, row);
        }
    }

    private void OnPointClicked(
        int point,
        bool isPlace,
        HashSet<int> legalPlaces,
        IReadOnlyList<NzengueEdgeDto> legalMoves)
    {
        if (!CanInteract || State?.CanAct != true)
            return;

        if (isPlace)
        {
            if (!legalPlaces.Contains(point))
                return;
            MoveRequested?.Invoke(this, (null, point));
            return;
        }

        var occupancy = State?.Occupancy ?? [];
        if (occupancy.Count <= point)
            return;

        if (_selectedFrom == point)
        {
            _selectedFrom = null;
            Render();
            return;
        }

        if (_selectedFrom is { } from)
        {
            if (legalMoves.Any(m => m.From == from && m.To == point))
            {
                MoveRequested?.Invoke(this, (from, point));
                _selectedFrom = null;
            }
            return;
        }

        if (occupancy[point] == 1 && legalMoves.Any(m => m.From == point))
        {
            _selectedFrom = point;
            Render();
        }
    }

    private static bool IsLegalFrom(int from, IReadOnlyList<NzengueEdgeDto> legalMoves) =>
        legalMoves.Any(m => m.From == from);

    private const string NzengueRulesPhasePlace = "place";
}
