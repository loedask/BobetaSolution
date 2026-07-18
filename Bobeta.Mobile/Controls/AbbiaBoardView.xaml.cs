using Bobeta.Client.Models.Api;

namespace Bobeta.Mobile.Controls;

public partial class AbbiaBoardView : ContentView
{
    private AbbiaStateDto? _abbia;
    private bool _canInteract;

    public AbbiaBoardView()
    {
        InitializeComponent();
    }

    public event EventHandler? ThrowRequested;

    public AbbiaStateDto? Abbia
    {
        get => _abbia;
        set
        {
            _abbia = value;
            Rebuild();
        }
    }

    public bool CanInteract
    {
        get => _canInteract;
        set
        {
            _canInteract = value;
            Rebuild();
        }
    }

    private void Rebuild()
    {
        Root.Children.Clear();
        var abbia = _abbia;
        var tokenCount = abbia?.TokenCount ?? 5;

        Root.Children.Add(BuildSeat(
            "Opponent",
            abbia?.OpponentHasThrown == true,
            abbia?.OpponentTokens,
            abbia?.OpponentCarvedUp,
            tokenCount));
        Root.Children.Add(BuildSeat(
            "You",
            abbia?.IHaveThrown == true,
            abbia?.MyTokens,
            abbia?.MyCarvedUp,
            tokenCount));

        if (_canInteract && abbia?.CanThrow == true)
        {
            var throwBtn = new Button
            {
                Text = "Throw tokens",
                CornerRadius = 12,
                HeightRequest = 44,
                BackgroundColor = Color.FromArgb("#C9A227"),
                TextColor = Color.FromArgb("#12151f")
            };
            throwBtn.Clicked += (_, _) => ThrowRequested?.Invoke(this, EventArgs.Empty);
            Root.Children.Add(throwBtn);
        }
    }

    private static View BuildSeat(
        string title,
        bool revealed,
        IReadOnlyList<bool>? tokens,
        int? carvedUp,
        int tokenCount)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(new Label
        {
            Text = title,
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#8a93a8"),
            HorizontalTextAlignment = TextAlignment.Center
        });

        var row = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center
        };
        if (revealed && tokens != null)
        {
            foreach (var carved in tokens)
            {
                row.Children.Add(new Label
                {
                    Text = carved ? "◆" : "○",
                    FontSize = 22,
                    TextColor = carved ? Color.FromArgb("#C9A227") : Color.FromArgb("#94a3b8")
                });
            }
        }
        else
        {
            for (var i = 0; i < tokenCount; i++)
            {
                row.Children.Add(new Label
                {
                    Text = "?",
                    FontSize = 22,
                    TextColor = Color.FromArgb("#64748b")
                });
            }
        }

        stack.Children.Add(row);
        stack.Children.Add(new Label
        {
            Text = revealed
                ? $"{carvedUp ?? 0} carved up"
                : "Waiting for throw",
            FontSize = 12,
            TextColor = Color.FromArgb("#e2e8f0"),
            HorizontalTextAlignment = TextAlignment.Center
        });
        return stack;
    }
}
