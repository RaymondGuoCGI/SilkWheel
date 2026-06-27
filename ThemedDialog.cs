using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SilkWheel;

public sealed class ThemedDialog : Window
{
    private readonly System.Windows.Controls.TextBox? _inputBox;
    private string? _result;

    private ThemedDialog(Window owner, string title, string message, string? defaultValue, bool inputMode, string okText, string cancelText)
    {
        Owner = owner;
        Title = title;
        Width = 420;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Background = System.Windows.Media.Brushes.Transparent;
        UseLayoutRounding = true;

        var cardBackground = Brush(owner, "CardBackground");
        var controlBackground = Brush(owner, "ControlBackground");
        var controlBorder = Brush(owner, "ControlBorder");
        var panelBorder = Brush(owner, "PanelBorder");
        var textPrimary = Brush(owner, "TextPrimary");
        var textSecondary = Brush(owner, "TextSecondary");
        var accent = Brush(owner, "Accent");

        var root = new Border
        {
            Background = cardBackground,
            BorderBrush = panelBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(18),
            SnapsToDevicePixels = true
        };

        var stack = new Grid();
        stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        stack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var titleBlock = new TextBlock
        {
            Text = title,
            Foreground = textPrimary,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        var closeButton = DialogButton("×", textPrimary, System.Windows.Media.Brushes.Transparent, System.Windows.Media.Brushes.Transparent, 34);
        closeButton.FontSize = 18;
        closeButton.Click += (_, _) => CloseWith(null);
        Grid.SetColumn(closeButton, 1);
        header.Children.Add(titleBlock);
        header.Children.Add(closeButton);

        var messageBlock = new TextBlock
        {
            Text = message,
            Foreground = textSecondary,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, inputMode ? 12 : 18)
        };

        Grid.SetRow(header, 0);
        Grid.SetRow(messageBlock, 1);
        stack.Children.Add(header);
        stack.Children.Add(messageBlock);

        if (inputMode)
        {
            _inputBox = new System.Windows.Controls.TextBox
            {
                Text = defaultValue ?? string.Empty,
                Height = 38,
                Padding = new Thickness(12, 8, 12, 0),
                Background = controlBackground,
                BorderBrush = controlBorder,
                Foreground = textPrimary,
                CaretBrush = accent,
                SelectionBrush = accent,
                Margin = new Thickness(0, 0, 0, 18)
            };
            _inputBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    CloseWith(_inputBox.Text);
                }
            };
            Grid.SetRow(_inputBox, 2);
            stack.Children.Add(_inputBox);
        }

        var buttons = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        var cancelButton = DialogButton(cancelText, textPrimary, controlBackground, controlBorder, 92);
        cancelButton.Margin = new Thickness(0, 0, 10, 0);
        cancelButton.Click += (_, _) => CloseWith(null);
        var okButton = DialogButton(okText, textPrimary, controlBackground, accent, 92);
        okButton.Click += (_, _) => CloseWith(inputMode ? _inputBox?.Text : "ok");
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(okButton);

        Grid.SetRow(buttons, 3);
        stack.Children.Add(buttons);
        root.Child = stack;
        Content = root;

        Loaded += (_, _) =>
        {
            if (_inputBox != null)
            {
                _inputBox.Focus();
                _inputBox.SelectAll();
            }
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                CloseWith(null);
            }
        };
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
    }

    public static string? ShowInput(Window owner, string title, string message, string defaultValue, string okText, string cancelText)
    {
        var dialog = new ThemedDialog(owner, title, message, defaultValue, inputMode: true, okText, cancelText);
        dialog.ShowDialog();
        return dialog._result;
    }

    public static bool Confirm(Window owner, string title, string message, string okText, string cancelText)
    {
        var dialog = new ThemedDialog(owner, title, message, null, inputMode: false, okText, cancelText);
        dialog.ShowDialog();
        return dialog._result == "ok";
    }

    private void CloseWith(string? result)
    {
        _result = string.IsNullOrWhiteSpace(result) ? null : result;
        DialogResult = _result != null;
        Close();
    }

    private static System.Windows.Controls.Button DialogButton(string text, System.Windows.Media.Brush foreground, System.Windows.Media.Brush background, System.Windows.Media.Brush border, double width)
    {
        var button = new System.Windows.Controls.Button
        {
            Content = text,
            Width = width,
            Height = 34,
            Foreground = foreground,
            Background = background,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        return button;
    }

    private static System.Windows.Media.Brush Brush(Window owner, string key)
    {
        return owner.Resources[key] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Transparent;
    }
}
