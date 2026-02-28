using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace deryaza.avalonia.themeconfiguration;

public partial class ColorPaletteEntryEditor : UserControl
{
    public static readonly StyledProperty<Color> ColorProperty = ColorView.ColorProperty.AddOwner<ColorPaletteEntryEditor>();

    public ColorPaletteEntryEditor()
    {
        InitializeComponent();
    }

    public Color Color
    {
        get => GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }
}