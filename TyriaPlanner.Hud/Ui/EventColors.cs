using Microsoft.Xna.Framework;
namespace TyriaPlanner.Hud.Ui
{
    public static class EventColors
    {
        public static Color For(string type)
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
    }
}
