using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using ColorM = Microsoft.Maui.Graphics.Color;

namespace Pictora.Components
{
    public class DraggableCaption : Grid
    {
        private double _originalX;
        private double _originalY;
        private double _totalX;
        private double _totalY;
        private Label _captionLabel;
        private Button _resizeHandle;
        private double _startWidth;
        private double _startHeight;
        private readonly EditImagePage _parentPage;

        public DraggableCaption(string text, EditImagePage parentPage)
        {
            _parentPage = parentPage;
            MinimumWidthRequest = 50;
            MinimumHeightRequest = 20;
            WidthRequest = 200;
            HeightRequest = 70;
            Padding = new Thickness(5);

            InitializeGrid();
            InitializeComponents(text);
            SetupGestureRecognizers();
        }

        private void InitializeGrid()
        {
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        private void InitializeComponents(string text)
        {
            _captionLabel = new Label
            {
                Text = text,
                TextColor = Colors.White,
                FontSize = 18,
                BackgroundColor = Colors.Black.WithAlpha(0.0f),
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill,
                LineBreakMode = LineBreakMode.WordWrap
            };

            _resizeHandle = new Button
            {
                Text = "⋰",
                FontSize = 14,
                TextColor = Colors.White,
                BackgroundColor = Colors.Transparent,
                WidthRequest = 24,
                HeightRequest = 24,
                Padding = new Thickness(0),
                Margin = new Thickness(0)
            };

            Add(_captionLabel);
            Add(_resizeHandle);
        }

        private void SetupGestureRecognizers()
        {
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnPanUpdated;
            GestureRecognizers.Add(panGesture);

            var resizePanGesture = new PanGestureRecognizer();
            resizePanGesture.PanUpdated += OnResizePanUpdated;
            _resizeHandle.GestureRecognizers.Add(resizePanGesture);

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += OnCaptionTapped;
            _captionLabel.GestureRecognizers.Add(tapGesture);

            var doubleTapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            _captionLabel.GestureRecognizers.Add(doubleTapGesture);
        }
        private async Task ChangeTextStyle()
        {
            if (_parentPage == null) return;

            string action = await _parentPage.DisplayActionSheet(
                "Choose Font Style",
                "Cancel",
                null,
                "Default",
                "Monospace",
                "Sans Serif",
                "Serif");

            if (string.IsNullOrEmpty(action) || action == "Cancel")
                return;

            _captionLabel.FontFamily = action switch
            {
                "Monospace" => "Courier New",
                "Sans Serif" => "Arial",
                "Serif" => "Times New Roman",
                _ => null // Default system font
            };
        }
        private async Task ChangeTextSize()
        {
            if (_parentPage == null) return;

            string action = await _parentPage.DisplayActionSheet(
                "Adjust Font Size",
                "Cancel",
                null,
                "Small (14)",
                "Medium (18)",
                "Large (24)",
                "Extra Large (32)");

            if (string.IsNullOrEmpty(action) || action == "Cancel")
                return;

            _captionLabel.FontSize = action switch
            {
                "Small (14)" => 14,
                "Medium (18)" => 18,
                "Large (24)" => 24,
                "Extra Large (32)" => 32,
                _ => _captionLabel.FontSize
            };
        }

        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _originalX = TranslationX;
                    _originalY = TranslationY;
                    break;

                case GestureStatus.Running:
                    TranslationX = _originalX + e.TotalX;
                    TranslationY = _originalY + e.TotalY;
                    _totalX = e.TotalX;
                    _totalY = e.TotalY;
                    break;

                case GestureStatus.Completed:
                    _originalX = TranslationX;
                    _originalY = TranslationY;
                    break;
            }
        }

        private void OnResizePanUpdated(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _startWidth = WidthRequest;
                    _startHeight = HeightRequest;
                    break;

                case GestureStatus.Running:
                    WidthRequest = Math.Min(Math.Max(_startWidth + e.TotalX, MinimumWidthRequest), 1024);
                    HeightRequest = Math.Min(Math.Max(_startHeight + e.TotalY, MinimumHeightRequest), 1024);
                    break;
            }
        }

        private async void OnCaptionTapped(object sender, EventArgs e)
        {
            if (_parentPage == null) return;

            string action = await _parentPage.DisplayActionSheet(
                "Caption Options",
                "Cancel",
                "Delete",
                "Edit Text",
                "Change Color",
                "Change Font Size",
                "Change Font Style");  // Added this option

            switch (action)
            {
                case "Delete":
                    _parentPage.DeleteCaption(this);
                    break;
                case "Edit Text":
                    await _parentPage.EditCaption(this);
                    break;
                case "Change Color":
                    await ChangeTextColor();
                    break;
                case "Change Font Size":
                    await ChangeTextSize();
                    break;
                case "Change Font Style":   // Added this case
                    await ChangeTextStyle();
                    break;
            }
        }

        private async Task ChangeTextColor()
        {
            if (_parentPage == null) return;

            var action = await _parentPage.DisplayActionSheet(
                "Select Text Color",
                "Cancel",
                null,
                "White", "Black", "Red", "Blue", "Green", "Yellow");

            ColorM newColor = action switch
            {
                "White" => Colors.White,
                "Black" => Colors.Black,
                "Red" => Colors.Red,
                "Blue" => Colors.Blue,
                "Green" => Colors.Green,
                "Yellow" => Colors.Yellow,
                _ => _captionLabel.TextColor
            };

            _captionLabel.TextColor = newColor;
            _captionLabel.BackgroundColor = IsLightColor(newColor) ?
                Colors.Black.WithAlpha(0.0f) :
                Colors.White.WithAlpha(0.0f);
        }

        private bool IsLightColor(ColorM color)
        {
            return (color.Red * 0.299 + color.Green * 0.587 + color.Blue * 0.114) > 0.5;
        }

        public string GetText() => _captionLabel?.Text ?? "";

        public void SetText(string newText)
        {
            if (_captionLabel != null)
            {
                _captionLabel.Text = newText;
            }
        }
    }
}