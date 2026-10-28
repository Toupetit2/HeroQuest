using UnityEngine;

public class Raycast
{
    public static bool CanSee(Map map, Vector2Int pos, Vector2Int target)
    {
        int steps = Mathf.Max(Mathf.Abs(target.x - pos.x), Mathf.Abs(target.y - pos.y));

        int prevX = pos.x;
        int prevY = pos.y;

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            int x = Mathf.RoundToInt(Mathf.Lerp(pos.x, target.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(pos.y, target.y, t));

            if (map.HasWall(prevX, prevY, x, y)) return false;

            prevX = x;
            prevY = y;
        }

        return true;
    }
}