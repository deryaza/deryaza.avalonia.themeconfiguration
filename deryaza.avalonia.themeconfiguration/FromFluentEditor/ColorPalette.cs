// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace FluentEditorShared;

public class ColorPalette
{
    public ColorScaleInterpolationMode InterpolationMode { get; set; } = ColorScaleInterpolationMode.RGB;

    public int Steps { get; set; } = 11;

    public Color ScaleColorLight { get; set; } = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);

    public Color ScaleColorDark { get; set; } = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

    public Color Color { get; set; }

    public double ClipLight { get; set; } = 0.185;

    public double ClipDark { get; set; } = 0.160;

    public double SaturationAdjustmentCutoff { get; set; } = 0.05;

    public double SaturationLight { get; set; } = 0.35;

    public double SaturationDark { get; set; } = 1.25;

    public double OverlayLight { get; set; } = 0.0;

    public double OverlayDark { get; set; } = 0.25;

    public double MultiplyLight { get; set; } = 0.0;

    public double MultiplyDark { get; set; } = 0.0;

    public ColorScale GetPaletteScale(Color color)
    {
        var baseColorRGB = color;
        var baseColorHSL = ColorUtils.RGBToHSL(baseColorRGB);
        var baseColorNormalized = new NormalizedRGB(baseColorRGB);

        var baseScale = new ColorScale(new Color[] { ScaleColorLight, baseColorRGB, ScaleColorDark, });

        var trimmedScale = baseScale.Trim(ClipLight, 1.0 - ClipDark);
        var trimmedLight = new NormalizedRGB(trimmedScale.GetColor(0, InterpolationMode));
        var trimmedDark = new NormalizedRGB(trimmedScale.GetColor(1, InterpolationMode));

        var adjustedLight = trimmedLight;
        var adjustedDark = trimmedDark;

        if (baseColorHSL.S >= SaturationAdjustmentCutoff)
        {
            adjustedLight = ColorBlending.SaturateViaLCH(adjustedLight, SaturationLight);
            adjustedDark = ColorBlending.SaturateViaLCH(adjustedDark, SaturationDark);
        }

        if (MultiplyLight != 0)
        {
            var multiply = ColorBlending.Blend(baseColorNormalized, adjustedLight, ColorBlendMode.Multiply);
            adjustedLight = ColorUtils.InterpolateColor(adjustedLight, multiply, MultiplyLight, InterpolationMode);
        }

        if (MultiplyDark != 0)
        {
            var multiply = ColorBlending.Blend(baseColorNormalized, adjustedDark, ColorBlendMode.Multiply);
            adjustedDark = ColorUtils.InterpolateColor(adjustedDark, multiply, MultiplyDark, InterpolationMode);
        }

        if (OverlayLight != 0)
        {
            var overlay = ColorBlending.Blend(baseColorNormalized, adjustedLight, ColorBlendMode.Overlay);
            adjustedLight = ColorUtils.InterpolateColor(adjustedLight, overlay, OverlayLight, InterpolationMode);
        }

        if (OverlayDark != 0)
        {
            var overlay = ColorBlending.Blend(baseColorNormalized, adjustedDark, ColorBlendMode.Overlay);
            adjustedDark = ColorUtils.InterpolateColor(adjustedDark, overlay, OverlayDark, InterpolationMode);
        }

        var finalScale = new ColorScale(new Color[] { adjustedLight.Denormalize(), baseColorRGB, adjustedDark.Denormalize() });
        return finalScale;
    }

    public IEnumerable<Color> UpdatePaletteColors()
    {
        var scale = GetPaletteScale(Color);

        for (int i = 0; i < Steps; i++)
        {
            var c = scale.GetColor((double)i / (double)(Steps - 1), InterpolationMode);
            yield return c;
        }
    }
}

public static class MathUtils
{
    public static byte ClampToByte(double c)
    {
        if (double.IsNaN(c))
        {
            return 0;
        }
        else if (double.IsPositiveInfinity(c))
        {
            return 255;
        }
        else if (double.IsNegativeInfinity(c))
        {
            return 0;
        }

        c = Math.Round(c);
        if (c <= 0)
        {
            return 0;
        }
        else if (c >= 255)
        {
            return 255;
        }
        else
        {
            return (byte)c;
        }
    }

    public static double ClampToUnit(double c)
    {
        if (double.IsNaN(c))
        {
            return 0;
        }
        else if (double.IsPositiveInfinity(c))
        {
            return 1;
        }
        else if (double.IsNegativeInfinity(c))
        {
            return 0;
        }

        if (c <= 0)
        {
            return 0;
        }
        else if (c >= 1)
        {
            return 1;
        }
        else
        {
            return c;
        }
    }

    public static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    public static double RadiansToDegrees(double radians)
    {
        return radians * (180.0 / Math.PI);
    }

    public static double Lerp(double left, double right, double scale)
    {
        if (scale <= 0)
        {
            return left;
        }
        else if (scale >= 1)
        {
            return right;
        }

        return left + scale * (right - left);
    }

    public static byte Lerp(byte left, byte right, double scale)
    {
        if (scale <= 0)
        {
            return left;
        }
        else if (scale >= 1)
        {
            return right;
        }
        else if (left == right)
        {
            return left;
        }

        double l = (double)left;
        double r = (double)right;
        return (byte)Math.Round(l + scale * (r - l));
    }
}

public static class ColorUtils
{
    public const int DefaultRoundingPrecision = 5;

    // This ignores the Alpha channel of the input color
    public static HSL RGBToHSL(Color rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        return RGBToHSL(new NormalizedRGB(rgb, false), round, roundingPrecision);
    }

    public static HSL RGBToHSL(in NormalizedRGB rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        double max = Math.Max(rgb.R, Math.Max(rgb.G, rgb.B));
        double min = Math.Min(rgb.R, Math.Min(rgb.G, rgb.B));
        double delta = max - min;

        double hue = 0;
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == rgb.R)
        {
            hue = 60 * (((rgb.G - rgb.B) / delta) % 6);
        }
        else if (max == rgb.G)
        {
            hue = 60 * ((rgb.B - rgb.R) / delta + 2);
        }
        else
        {
            hue = 60 * ((rgb.R - rgb.G) / delta + 4);
        }

        if (hue < 0)
        {
            hue += 360;
        }

        double lit = (max + min) / 2;

        double sat = 0;
        if (delta != 0)
        {
            sat = delta / (1 - Math.Abs(2 * lit - 1));
        }

        return new HSL(hue, sat, lit, round, roundingPrecision);
    }

    public static LAB LCHToLAB(in LCH lch, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        // LCH lit == LAB lit
        double a = 0;
        double b = 0;
        // In chroma.js this case is handled by having the rgb -> lch conversion special case h == 0. In that case it changes h to NaN. Which then requires some NaN checks elsewhere.
        // it seems preferable to handle the case of h = 0 here
        if (lch.H != 0)
        {
            a = Math.Cos(MathUtils.DegreesToRadians(lch.H)) * lch.C;
            b = Math.Sin(MathUtils.DegreesToRadians(lch.H)) * lch.C;
        }

        return new LAB(lch.L, a, b, round, roundingPrecision);
    }

    // This discontinuity in the C parameter at 0 means that floating point errors will often result in values near 0 giving unpredictable results. 
    // EG: 0.0000001 gives a very different result than -0.0000001
    public static LCH LABToLCH(in LAB lab, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        // LCH lit == LAB lit
        double h = (MathUtils.RadiansToDegrees(Math.Atan2(lab.B, lab.A)) + 360) % 360;
        double c = Math.Sqrt(lab.A * lab.A + lab.B * lab.B);

        return new LCH(lab.L, c, h, round, roundingPrecision);
    }

    // This conversion uses the D65 constants for 2 degrees. That determines the constants used for the pure white point of the XYZ space of 0.95047, 1.0, 1.08883
    public static XYZ LABToXYZ(in LAB lab, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        double y = (lab.L + 16.0) / 116.0;
        double x = y + (lab.A / 500.0);
        double z = y - (lab.B / 200.0);

        double LABToXYZHelper(double i)
        {
            if (i > 0.206896552)
            {
                return Math.Pow(i, 3);
            }
            else
            {
                return 0.12841855 * (i - 0.137931034);
            }
        }

        x = 0.95047 * LABToXYZHelper(x);
        y = LABToXYZHelper(y);
        z = 1.08883 * LABToXYZHelper(z);

        return new XYZ(x, y, z, round, roundingPrecision);
    }

    // This conversion uses the D65 constants for 2 degrees. That determines the constants used for the pure white point of the XYZ space of 0.95047, 1.0, 1.08883
    public static LAB XYZToLAB(in XYZ xyz, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        double XYZToLABHelper(double i)
        {
            if (i > 0.008856452)
            {
                return Math.Pow(i, 1.0 / 3.0);
            }
            else
            {
                return i / 0.12841855 + 0.137931034;
            }
        }

        double x = XYZToLABHelper(xyz.X / 0.95047);
        double y = XYZToLABHelper(xyz.Y);
        double z = XYZToLABHelper(xyz.Z / 1.08883);

        double l = (116.0 * y) - 16.0;
        double a = 500.0 * (x - y);
        double b = -200.0 * (z - y);

        return new LAB(l, a, b, round, roundingPrecision);
    }

    // This ignores the Alpha channel of the input color
    public static XYZ RGBToXYZ(Color rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        return RGBToXYZ(new NormalizedRGB(rgb, false), round, roundingPrecision);
    }

    public static XYZ RGBToXYZ(in NormalizedRGB rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        double RGBToXYZHelper(double i)
        {
            if (i <= 0.04045)
            {
                return i / 12.92;
            }
            else
            {
                return Math.Pow((i + 0.055) / 1.055, 2.4);
            }
        }

        double r = RGBToXYZHelper(rgb.R);
        double g = RGBToXYZHelper(rgb.G);
        double b = RGBToXYZHelper(rgb.B);

        double x = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
        double y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
        double z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

        return new XYZ(x, y, z, round, roundingPrecision);
    }

    public static NormalizedRGB XYZToRGB(in XYZ xyz, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        double XYZToRGBHelper(double i)
        {
            if (i <= 0.0031308)
            {
                return i * 12.92;
            }
            else
            {
                return 1.055 * Math.Pow(i, 1 / 2.4) - 0.055;
            }
        }

        double r = XYZToRGBHelper(xyz.X * 3.2404542 - xyz.Y * 1.5371385 - xyz.Z * 0.4985314);
        double g = XYZToRGBHelper(xyz.X * -0.9692660 + xyz.Y * 1.8760108 + xyz.Z * 0.0415560);
        double b = XYZToRGBHelper(xyz.X * 0.0556434 - xyz.Y * 0.2040259 + xyz.Z * 1.0572252);

        return new NormalizedRGB(MathUtils.ClampToUnit(r), MathUtils.ClampToUnit(g), MathUtils.ClampToUnit(b), round, roundingPrecision);
    }

    // This ignores the Alpha channel of the input color
    public static LAB RGBToLAB(Color rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        return RGBToLAB(new NormalizedRGB(rgb, false), round, roundingPrecision);
    }

    public static LAB RGBToLAB(in NormalizedRGB rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        XYZ xyz = RGBToXYZ(rgb, false);
        return XYZToLAB(xyz, round, roundingPrecision);
    }

    public static NormalizedRGB LABToRGB(in LAB lab, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        XYZ xyz = LABToXYZ(lab, false);
        return XYZToRGB(xyz, round, roundingPrecision);
    }

    public static LCH RGBToLCH(in NormalizedRGB rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        // The discontinuity near 0 in LABToLCH means we should round here even if the bound param is false
        LAB lab = RGBToLAB(rgb, true, 4);

        // This appears redundant but is actually nescessary in order to prevent floating point rounding errors from throwing off the Atan2 computation in LABToLCH
        // https://msdn.microsoft.com/en-us/library/system.math.atan2(v=vs.110).aspx
        // For the RGB value 255,255,255 what happens is the a value appears to be rounded to 0 but is still treated as negative by Atan2 which then returns PI instead of 0

        double l = lab.L == 0 ? 0 : lab.L;
        double a = lab.A == 0 ? 0 : lab.A;
        double b = lab.B == 0 ? 0 : lab.B;

        return LABToLCH(new LAB(l, a, b, false), round, roundingPrecision);
    }

    public static NormalizedRGB LCHToRGB(in LCH lch, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        LAB lab = LCHToLAB(lch, false);
        return LABToRGB(lab, round, roundingPrecision);
    }

    public static Color InterpolateRGB(Color left, Color right, double position)
    {
        if (position <= 0)
        {
            return left;
        }

        if (position >= 1)
        {
            return right;
        }

        return Color.FromArgb(MathUtils.Lerp(left.A, right.A, position), MathUtils.Lerp(left.R, right.R, position), MathUtils.Lerp(left.G, right.G, position), MathUtils.Lerp(left.B, right.B, position));
    }

    public static NormalizedRGB InterpolateRGB(in NormalizedRGB left, in NormalizedRGB right, double position)
    {
        if (position <= 0)
        {
            return left;
        }

        if (position >= 1)
        {
            return right;
        }

        return new NormalizedRGB(MathUtils.Lerp(left.R, right.R, position), MathUtils.Lerp(left.G, right.G, position), MathUtils.Lerp(left.B, right.B, position), false);
    }

    // Generally looks better than RGB for interpolation
    public static LAB InterpolateLAB(in LAB left, in LAB right, double position)
    {
        if (position <= 0)
        {
            return left;
        }

        if (position >= 1)
        {
            return right;
        }

        return new LAB(MathUtils.Lerp(left.L, right.L, position), MathUtils.Lerp(left.A, right.A, position), MathUtils.Lerp(left.B, right.B, position), false);
    }

    // Possibly a better choice than LAB for very dark colors
    public static XYZ InterpolateXYZ(in XYZ left, in XYZ right, double position)
    {
        if (position <= 0)
        {
            return left;
        }

        if (position >= 1)
        {
            return right;
        }

        return new XYZ(MathUtils.Lerp(left.X, right.X, position), MathUtils.Lerp(left.Y, right.Y, position), MathUtils.Lerp(left.Z, right.Z, position), false);
    }

    public static NormalizedRGB InterpolateColor(in NormalizedRGB left, in NormalizedRGB right, double position, ColorScaleInterpolationMode mode)
    {
        switch (mode)
        {
            case ColorScaleInterpolationMode.LAB:
                var leftLAB = ColorUtils.RGBToLAB(left, false);
                var rightLAB = ColorUtils.RGBToLAB(right, false);
                return LABToRGB(InterpolateLAB(leftLAB, rightLAB, position));
            case ColorScaleInterpolationMode.XYZ:
                var leftXYZ = RGBToXYZ(left, false);
                var rightXYZ = RGBToXYZ(right, false);
                return XYZToRGB(InterpolateXYZ(leftXYZ, rightXYZ, position));
            default:
                return InterpolateRGB(left, right, position);
        }
    }
}

public static class ColorBlending
{
    public const double DefaultSaturationConstant = 18.0;

    public static NormalizedRGB SaturateViaLCH(in NormalizedRGB input, double saturation, double saturationConstant = DefaultSaturationConstant)
    {
        LCH lch = ColorUtils.RGBToLCH(input, false);
        double saturated = lch.C + saturation * saturationConstant;
        if (saturated < 0)
        {
            saturated = 0;
        }

        return ColorUtils.LCHToRGB(new LCH(lch.L, saturated, lch.H, false), false);
    }

    public static NormalizedRGB Blend(in NormalizedRGB bottom, in NormalizedRGB top, ColorBlendMode mode)
    {
        switch (mode)
        {
            case ColorBlendMode.Burn:
                return BlendBurn(bottom, top);
            case ColorBlendMode.Darken:
                return BlendDarken(bottom, top);
            case ColorBlendMode.Dodge:
                return BlendDodge(bottom, top);
            case ColorBlendMode.Lighten:
                return BlendLighten(bottom, top);
            case ColorBlendMode.Multiply:
                return BlendMultiply(bottom, top);
            case ColorBlendMode.Overlay:
                return BlendOverlay(bottom, top);
            case ColorBlendMode.Screen:
                return BlendScreen(bottom, top);
            default:
                throw new ArgumentException("Unknown blend mode", "mode");
        }
    }

    public static NormalizedRGB BlendBurn(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendBurn(bottom.R, top.R), BlendBurn(bottom.G, top.G), BlendBurn(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendBurn(double bottom, double top)
    {
        if (top == 0.0)
        {
            // Despite the discontinuity, other sources seem to use 0.0 here instead of 1
            return 0.0;
        }

        return 1.0 - (1.0 - bottom) / top;
    }

    public static NormalizedRGB BlendDarken(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendDarken(bottom.R, top.R), BlendDarken(bottom.G, top.G), BlendDarken(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendDarken(double bottom, double top)
    {
        return Math.Min(bottom, top);
    }

    public static NormalizedRGB BlendDodge(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendDodge(bottom.R, top.R), BlendDodge(bottom.G, top.G), BlendDodge(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendDodge(double bottom, double top)
    {
        if (top >= 1.0)
        {
            return 1.0;
        }

        double retVal = bottom / (1.0 - top);
        if (retVal >= 1.0)
        {
            return 1.0;
        }

        return retVal;
    }

    public static NormalizedRGB BlendLighten(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendLighten(bottom.R, top.R), BlendLighten(bottom.G, top.G), BlendLighten(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendLighten(double bottom, double top)
    {
        return Math.Max(bottom, top);
    }

    public static NormalizedRGB BlendMultiply(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendMultiply(bottom.R, top.R), BlendMultiply(bottom.G, top.G), BlendMultiply(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendMultiply(double bottom, double top)
    {
        return bottom * top;
    }

    public static NormalizedRGB BlendOverlay(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendOverlay(bottom.R, top.R), BlendOverlay(bottom.G, top.G), BlendOverlay(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendOverlay(double bottom, double top)
    {
        if (bottom < 0.5)
        {
            return MathUtils.ClampToUnit(2.0 * top * bottom);
        }
        else
        {
            return MathUtils.ClampToUnit(1.0 - 2.0 * (1.0 - top) * (1.0 - bottom));
        }
    }

    public static NormalizedRGB BlendScreen(in NormalizedRGB bottom, in NormalizedRGB top)
    {
        return new NormalizedRGB(BlendScreen(bottom.R, top.R), BlendScreen(bottom.G, top.G), BlendScreen(bottom.B, top.B), false);
    }

    // single channel in the range [0.0,1.0]
    public static double BlendScreen(double bottom, double top)
    {
        return 1.0 - (1.0 - top) * (1.0 - bottom);
    }
}

// Valid values for each channel are ∈ [0.0,1.0]
// But sometimes it is useful to allow values outside that range during calculations as long as they are clamped eventually
public readonly struct NormalizedRGB : IEquatable<NormalizedRGB>
{
    public const int DefaultRoundingPrecision = 5;

    public NormalizedRGB(double r, double g, double b, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            R = Math.Round(r, roundingPrecision);
            G = Math.Round(g, roundingPrecision);
            B = Math.Round(b, roundingPrecision);
        }
        else
        {
            R = r;
            G = g;
            B = b;
        }
    }

    public NormalizedRGB(in Color rgb, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            R = Math.Round((double)rgb.R / 255.0, roundingPrecision);
            G = Math.Round((double)rgb.G / 255.0, roundingPrecision);
            B = Math.Round((double)rgb.B / 255.0, roundingPrecision);
        }
        else
        {
            R = (double)rgb.R / 255.0;
            G = (double)rgb.G / 255.0;
            B = (double)rgb.B / 255.0;
        }
    }

    public Color Denormalize(byte a = 255)
    {
        return Color.FromArgb(a, MathUtils.ClampToByte(R * 255.0), MathUtils.ClampToByte(G * 255.0), MathUtils.ClampToByte(B * 255.0));
    }

    public readonly double R;
    public readonly double G;
    public readonly double B;

    #region IEquatable<NormalizedRGB>

    public bool Equals(NormalizedRGB other)
    {
        return R == other.R && G == other.G && B == other.B;
    }

    #endregion

    #region Equals

    public override bool Equals(object obj)
    {
        if (obj is NormalizedRGB other)
        {
            return R == other.R && G == other.G && B == other.B;
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return string.Format("{0},{1},{2}", R, G, B);
    }

    #endregion
}

// H ∈ [0.0,360.0]
// S ∈ [0.0,1.0]
// L ∈ [0.0,1.0]
public readonly struct HSL : IEquatable<HSL>
{
    public const int DefaultRoundingPrecision = 5;

    public HSL(double h, double s, double l, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            H = Math.Round(h, roundingPrecision);
            S = Math.Round(s, roundingPrecision);
            L = Math.Round(l, roundingPrecision);
        }
        else
        {
            H = h;
            S = s;
            L = l;
        }
    }

    public readonly double H;
    public readonly double S;
    public readonly double L;

    #region IEquatable<HSL>

    public bool Equals(HSL other)
    {
        return H == other.H && S == other.S && L == other.L;
    }

    #endregion

    #region Equals

    public override bool Equals(object obj)
    {
        if (obj is HSL other)
        {
            return H == other.H && S == other.S && L == other.L;
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return H.GetHashCode() ^ S.GetHashCode() ^ L.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return string.Format("{0},{1},{2}", H, S, L);
    }

    #endregion
}

public readonly struct LAB : IEquatable<LAB>
{
    public const int DefaultRoundingPrecision = 5;

    public LAB(double l, double a, double b, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            L = Math.Round(l, roundingPrecision);
            A = Math.Round(a, roundingPrecision);
            B = Math.Round(b, roundingPrecision);
        }
        else
        {
            L = l;
            A = a;
            B = b;
        }
    }

    public readonly double L;
    public readonly double A;
    public readonly double B;

    #region IEquatable<LAB>

    public bool Equals(LAB other)
    {
        return L == other.L && A == other.A && B == other.B;
    }

    #endregion

    #region Equals

    public override bool Equals(object obj)
    {
        if (obj is LAB other)
        {
            return L == other.L && A == other.A && B == other.B;
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return L.GetHashCode() ^ A.GetHashCode() ^ B.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return string.Format("{0},{1},{2}", L, A, B);
    }

    #endregion
}

public readonly struct LCH : IEquatable<LCH>
{
    public const int DefaultRoundingPrecision = 5;

    public LCH(double l, double c, double h, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            L = Math.Round(l, roundingPrecision);
            C = Math.Round(c, roundingPrecision);
            H = Math.Round(h, roundingPrecision);
        }
        else
        {
            L = l;
            C = c;
            H = h;
        }
    }

    public readonly double L;
    public readonly double C;
    public readonly double H;

    #region IEquatable<LCH>

    public bool Equals(LCH other)
    {
        return L == other.L && C == other.C && H == other.H;
    }

    #endregion

    #region Equals

    public override bool Equals(object obj)
    {
        if (obj is LCH other)
        {
            return L == other.L && C == other.C && H == other.H;
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return L.GetHashCode() ^ C.GetHashCode() ^ H.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return string.Format("{0},{1},{2}", L, C, H);
    }

    #endregion
}

public readonly struct XYZ : IEquatable<XYZ>
{
    public const int DefaultRoundingPrecision = 5;

    public XYZ(double x, double y, double z, bool round = true, int roundingPrecision = DefaultRoundingPrecision)
    {
        if (round)
        {
            X = Math.Round(x, roundingPrecision);
            Y = Math.Round(y, roundingPrecision);
            Z = Math.Round(z, roundingPrecision);
        }
        else
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    #region IEquatable<XYZ>

    public bool Equals(XYZ other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    #endregion

    #region Equals

    public override bool Equals(object obj)
    {
        if (obj is XYZ other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
        else
        {
            return base.Equals(obj);
        }
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        return string.Format("{0},{1},{2}", X, Y, Z);
    }

    #endregion
}

public enum ColorScaleInterpolationMode
{
    RGB,
    LAB,
    XYZ
};

public enum ColorBlendMode
{
    Burn,
    Darken,
    Dodge,
    Lighten,
    Multiply,
    Overlay,
    Screen
};

public class ColorScale
{
    // Evenly distributes the colors provided between 0 and 1
    public ColorScale(IEnumerable<Color> colors)
    {
        if (colors == null)
        {
            throw new ArgumentNullException("colors");
        }

        int count = colors.Count();
        _stops = new ColorScaleStop[count];
        int index = 0;
        foreach (Color color in colors)
        {
            // Clean up floating point jaggies
            if (index == 0)
            {
                _stops[index] = new ColorScaleStop(color, 0);
            }
            else if (index == count - 1)
            {
                _stops[index] = new ColorScaleStop(color, 1);
            }
            else
            {
                _stops[index] = new ColorScaleStop(color, (double)index * (1.0 / (double)(count - 1)));
            }

            index++;
        }
    }

    public ColorScale(IEnumerable<ColorScaleStop> stops)
    {
        if (stops == null)
        {
            throw new ArgumentNullException("stops");
        }

        int count = stops.Count();
        _stops = new ColorScaleStop[count];
        int index = 0;
        foreach (ColorScaleStop stop in stops)
        {
            _stops[index] = new ColorScaleStop(stop);
            index++;
        }
    }

    private readonly ColorScaleStop[] _stops;

    public Color GetColor(double position, ColorScaleInterpolationMode mode = ColorScaleInterpolationMode.RGB)
    {
        if (_stops.Length == 1)
        {
            return _stops[0].Color;
        }

        if (position <= 0)
        {
            return _stops[0].Color;
        }
        else if (position >= 1)
        {
            return _stops[_stops.Length - 1].Color;
        }

        int lowerIndex = 0;
        for (int i = 0; i < _stops.Length; i++)
        {
            if (_stops[i].Position <= position)
            {
                lowerIndex = i;
            }
        }

        int upperIndex = lowerIndex + 1;
        if (upperIndex >= _stops.Length)
        {
            upperIndex = _stops.Length - 1;
        }

        double scalePosition = (position - _stops[lowerIndex].Position) * (1.0 / (_stops[upperIndex].Position - _stops[lowerIndex].Position));

        switch (mode)
        {
            case ColorScaleInterpolationMode.LAB:
                LAB leftLAB = ColorUtils.RGBToLAB(_stops[lowerIndex].Color, false);
                LAB rightLAB = ColorUtils.RGBToLAB(_stops[upperIndex].Color, false);
                LAB targetLAB = ColorUtils.InterpolateLAB(leftLAB, rightLAB, scalePosition);
                return ColorUtils.LABToRGB(targetLAB, false).Denormalize();
            case ColorScaleInterpolationMode.XYZ:
                XYZ leftXYZ = ColorUtils.RGBToXYZ(_stops[lowerIndex].Color, false);
                XYZ rightXYZ = ColorUtils.RGBToXYZ(_stops[upperIndex].Color, false);
                XYZ targetXYZ = ColorUtils.InterpolateXYZ(leftXYZ, rightXYZ, scalePosition);
                return ColorUtils.XYZToRGB(targetXYZ, false).Denormalize();
            default:
                return ColorUtils.InterpolateRGB(_stops[lowerIndex].Color, _stops[upperIndex].Color, scalePosition);
        }
    }

    public ColorScale Trim(double lowerBound, double upperBound, ColorScaleInterpolationMode mode = ColorScaleInterpolationMode.RGB)
    {
        if (lowerBound < 0 || upperBound > 1 || upperBound < lowerBound)
        {
            throw new ArgumentException("Invalid bounds");
        }

        if (lowerBound == upperBound)
        {
            return new ColorScale(new Color[] { GetColor(lowerBound, mode) });
        }

        List<ColorScaleStop> containedStops = new List<ColorScaleStop>(_stops.Length);

        for (int i = 0; i < _stops.Length; i++)
        {
            if (_stops[i].Position >= lowerBound && _stops[i].Position <= upperBound)
            {
                containedStops.Add(_stops[i]);
            }
        }

        if (containedStops.Count == 0)
        {
            return new ColorScale(new Color[] { GetColor(lowerBound, mode), GetColor(upperBound, mode) });
        }

        if (containedStops.First().Position != lowerBound)
        {
            containedStops.Insert(0, new ColorScaleStop(GetColor(lowerBound, mode), lowerBound));
        }

        if (containedStops.Last().Position != upperBound)
        {
            containedStops.Add(new ColorScaleStop(GetColor(upperBound, mode), upperBound));
        }

        double range = upperBound - lowerBound;
        ColorScaleStop[] finalStops = new ColorScaleStop[containedStops.Count];
        for (int i = 0; i < finalStops.Length; i++)
        {
            double adjustedPosition = (containedStops[i].Position - lowerBound) / range;
            finalStops[i] = new ColorScaleStop(containedStops[i].Color, adjustedPosition);
        }

        return new ColorScale(finalStops);
    }
}

public readonly struct ColorScaleStop
{
    public ColorScaleStop(Color color, double position)
    {
        Color = color;
        Position = position;
    }

    public ColorScaleStop(ColorScaleStop source)
    {
        Color = source.Color;
        Position = source.Position;
    }

    public readonly Color Color;
    public readonly double Position;
}