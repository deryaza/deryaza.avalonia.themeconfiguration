using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using FluentEditorShared;

namespace deryaza.avalonia.themeconfiguration;

public partial class ControlPaletteTestContent : UserControl
{
    public ControlPaletteTestContent()
    {
        InitializeComponent();
        var colorPalettes = new List<ColorPalette>();
        for (int i = 0; i < 100; i++)
        {
            colorPalettes.Add(new ColorPalette
            {
                InterpolationMode = ColorScaleInterpolationMode.RGB,
                Steps = Random.Shared.Next(0, int.MaxValue),
                ScaleColorLight = Color.FromUInt32((uint)Random.Shared.Next(0, int.MaxValue)),
                ScaleColorDark = Color.FromUInt32((uint)Random.Shared.Next(0, int.MaxValue)),
                Color = Color.FromUInt32((uint)Random.Shared.Next(0, int.MaxValue)),
                ClipLight = Random.Shared.Next(int.MinValue, int.MaxValue),
                ClipDark = Random.Shared.Next(int.MinValue, int.MaxValue),
                SaturationAdjustmentCutoff = Random.Shared.Next(0, int.MaxValue),
                SaturationLight = Random.Shared.Next(int.MinValue, int.MaxValue),
                SaturationDark = Random.Shared.Next(int.MinValue, int.MaxValue),
                OverlayLight = Random.Shared.Next(int.MinValue, int.MaxValue),
                OverlayDark = Random.Shared.Next(int.MinValue, int.MaxValue),
                MultiplyLight = Random.Shared.Next(int.MinValue, int.MaxValue),
                MultiplyDark = Random.Shared.Next(int.MinValue, int.MaxValue)
            });
        }

        DataGrid.ItemsSource = colorPalettes;
    }
}