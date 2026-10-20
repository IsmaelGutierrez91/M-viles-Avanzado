using UnityEngine;

public static class Paleta
{
    public static Color ColorDe(ulong id)
    {
        return Color.HSVToRGB((id * 0.27f) % 1f, 0.75f, 0.95f);
    }
}
