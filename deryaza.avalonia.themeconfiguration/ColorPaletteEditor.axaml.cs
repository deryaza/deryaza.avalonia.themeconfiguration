using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace deryaza.avalonia.themeconfiguration;

public partial class ColorPaletteEditor : UserControl
{
    public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<ColorPaletteEditor>();

    public static readonly StyledProperty<Color> LightColorProperty = AvaloniaProperty.Register<ColorPaletteEditor, Color>(nameof(LightColor), defaultBindingMode: BindingMode.TwoWay);
    public static readonly StyledProperty<Color> DarkColorProperty = AvaloniaProperty.Register<ColorPaletteEditor, Color>(nameof(DarkColor), defaultBindingMode: BindingMode.TwoWay);

    public ColorPaletteEditor()
    {
        InitializeComponent();
    }

    public Color DarkColor
    {
        get => GetValue(DarkColorProperty);
        set => SetValue(DarkColorProperty, value);
    }

    public Color LightColor
    {
        get => GetValue(LightColorProperty);
        set => SetValue(LightColorProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}