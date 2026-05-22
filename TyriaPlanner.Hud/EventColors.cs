using Microsoft.Xna.Framework;
using TyriaPlanner.Hud.Settings;

namespace TyriaPlanner.Hud.Ui
{
    public static class EventColors
    {
        // Theme is read on each call · the dropdown live-updates without a
        // module reload because every label/panel pulls the color afresh on
        // its next layout pass.
        public static Color For(string type, ColorThemePreference theme = ColorThemePreference.Default)
        {
            switch (theme)
            {
                case ColorThemePreference.HighContrast:
                    return HighContrast(type);
                case ColorThemePreference.Pastel:
                    return Pastel(type);
                case ColorThemePreference.Monochrome:
                    return Monochrome(type);
                default:
                    return Default(type);
            }
        }

        private static Color Default(string type)
        {
            switch (type)
            {
                case "raid":       return new Color(218, 165,  32);
                case "fractal":    return new Color( 70, 180,  90);
                case "strike":     return new Color( 60, 140, 230);
                case "wvw":        return new Color(220,  70,  60);
                case "open_world": return new Color(160,  90, 200);
                default:           return new Color(180, 180, 180);
            }
        }

        private static Color HighContrast(string type)
        {
            switch (type)
            {
                case "raid":       return new Color(255, 210,  60);
                case "fractal":    return new Color( 80, 230, 110);
                case "strike":     return new Color( 80, 180, 255);
                case "wvw":        return new Color(255,  90,  80);
                case "open_world": return new Color(210, 110, 255);
                default:           return new Color(220, 220, 220);
            }
        }

        private static Color Pastel(string type)
        {
            switch (type)
            {
                case "raid":       return new Color(230, 200, 130);
                case "fractal":    return new Color(150, 210, 165);
                case "strike":     return new Color(140, 180, 230);
                case "wvw":        return new Color(230, 150, 145);
                case "open_world": return new Color(195, 165, 220);
                default:           return new Color(200, 200, 200);
            }
        }

        private static Color Monochrome(string type)
        {
            return new Color(218, 165, 32);
        }
    }
}
