using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using DynamicData.Binding;
using FluentEditorShared;
using ReactiveUI;

namespace deryaza.avalonia.themeconfiguration.ViewModels;

public class MainViewModel : ReactiveObject
{
    public MainViewModel()
    {
        this.WhenAnyPropertyChanged().Subscribe(OnNext);

        FluentTheme? existingFluentTheme = App.Current.Styles.OfType<FluentTheme>().FirstOrDefault();

        RegionLightColor = existingFluentTheme.TryGetResource("SystemRegionColor", ThemeVariant.Light, out object value) ? (Color)value : default;
        RegionDarkColor = existingFluentTheme.TryGetResource("SystemRegionColor", ThemeVariant.Dark, out value) ? (Color)value : default;

        BaseLightColor = existingFluentTheme.TryGetResource("SystemBaseLowColor", ThemeVariant.Light, out value) ? (Color)value : default;
        BaseDarkColor = existingFluentTheme.TryGetResource("SystemBaseLowColor", ThemeVariant.Dark, out value) ? (Color)value : default;

        PrimaryLightColor = existingFluentTheme.TryGetResource("SystemAccentColor", ThemeVariant.Light, out value) ? (Color)value : default;
        PrimaryDarkColor = existingFluentTheme.TryGetResource("SystemAccentColor", ThemeVariant.Dark, out value) ? (Color)value : default;
    }

    public Color RegionLightColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Color RegionDarkColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Color BaseLightColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Color BaseDarkColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Color PrimaryLightColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public Color PrimaryDarkColor
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private void OnNext(MainViewModel? obj)
    {
        ColorPaletteResources lightPalette;
        ColorPaletteResources darkPalette;

        var baseColors = new ColorPalette() { Color = BaseDarkColor }.UpdatePaletteColors().ToArray();
        var primaryColors = new ColorPalette() { Color = PrimaryDarkColor }.UpdatePaletteColors().ToArray();
        darkPalette = new ColorPaletteResources
        {
            RegionColor = RegionDarkColor,
            Accent = primaryColors[5],
            AltHigh = Colors.Black,
            AltLow = Colors.Black,
            AltMedium = Colors.Black,
            AltMediumHigh = Colors.Black,
            AltMediumLow = Colors.Black,
            BaseHigh = Colors.White,
            BaseLow = baseColors[5],
            BaseMedium = baseColors[1],
            BaseMediumHigh = baseColors[0],
            BaseMediumLow = baseColors[3],
            ChromeAltLow = baseColors[0],
            ChromeBlackHigh = Colors.Black,
            ChromeBlackLow = baseColors[0],
            ChromeBlackMedium = Colors.Black,
            ChromeBlackMediumLow = Colors.Black,
            ChromeDisabledHigh = baseColors[5],
            ChromeDisabledLow = baseColors[1],
            ChromeGray = baseColors[2],
            ChromeHigh = baseColors[2],
            ChromeLow = baseColors[9],
            ChromeMedium = baseColors[8],
            ChromeMediumLow = baseColors[6],
            ChromeWhite = Colors.White,
            ListLow = baseColors[8],
            ListMedium = baseColors[5],
        };

        baseColors = new ColorPalette() { Color = BaseLightColor }.UpdatePaletteColors().ToArray();
        primaryColors = new ColorPalette() { Color = PrimaryLightColor }.UpdatePaletteColors().ToArray();
        lightPalette = new ColorPaletteResources()
        {
            RegionColor = RegionLightColor,
            Accent = primaryColors[5],
            AltHigh = Colors.White,
            AltLow = Colors.White,
            AltMedium = Colors.White,
            AltMediumHigh = Colors.White,
            AltMediumLow = Colors.White,
            BaseHigh = Colors.Black,
            BaseLow = baseColors[5],
            BaseMedium = baseColors[8],
            BaseMediumHigh = baseColors[10],
            BaseMediumLow = baseColors[9],
            ChromeAltLow = baseColors[10],
            ChromeBlackHigh = Colors.Black,
            ChromeBlackLow = baseColors[5],
            ChromeBlackMedium = baseColors[10],
            ChromeBlackMediumLow = baseColors[8],
            ChromeDisabledHigh = baseColors[5],
            ChromeDisabledLow = baseColors[8],
            ChromeGray = baseColors[9],
            ChromeHigh = baseColors[5],
            ChromeLow = baseColors[0],
            ChromeMedium = baseColors[1],
            ChromeMediumLow = baseColors[0],
            ChromeWhite = Colors.White,
            ListLow = baseColors[1],
            ListMedium = baseColors[5],
        };

        var app = Application.Current;
        var existingFluentTheme = app.Styles.OfType<FluentTheme>().FirstOrDefault();
        if (existingFluentTheme != null)
        {
            app.Styles.Remove(existingFluentTheme);
        }

        var newTheme = new FluentTheme
        {
            Palettes =
            {
                [ThemeVariant.Light] = lightPalette,
                [ThemeVariant.Dark] = darkPalette,
            }
        };

        app.Styles.Add(newTheme);
    }
}