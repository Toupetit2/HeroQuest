using System.Collections.Generic;
using UnityEngine;

public class Pathfinding
{
    private static bool IsOccupied(GameManager gameManager, Vector2Int position) 
    {
        GameState gameState = gameManager.GetGameState();

        foreach (Hero hero in gameState.heroList) 
        {
            if (hero != gameManager.GetCurrentHero() && !hero.isDead)
            {
                if (hero.position == position) 
                {
                    return true;
                }
            }
        }
        foreach (Monster monster in gameState.monsterList) 
        {
            if (monster.position == position) 
            {
                return true;
            }
        }
        return false;
    }

    public static bool CanStep(GameManager gameManager, Vector2Int from, Vector2Int direction, Vector2Int to)
    {
        Map map = gameManager.GetMap();
        Hero hero = gameManager.GetCurrentHero();

        if (!map.InBounds(to.x, to.y))
            return false;

        if (map.HasWall(from.x, from.y, direction.x, direction.y))
            return false;

        // la roche pleine est inaccessible pour tout le monde (meme un fantome)
        if (map.tiles[to.x, to.y].rock)
            return false;

        if (!hero.isGhosted) 
        {
            if (!map.tiles[to.x, to.y].isWalkable())
                return false;
        }
        
        if (!hero.isInvisible) 
        {
            if (IsOccupied(gameManager, direction))
                return false;
        }

        return true;
    }

    private static List<Vector2Int> FindPath(GameManager gameManager, Vector2Int start, Vector2Int destination)
    {
        Map map = gameManager.GetMap();

        Queue <Vector2Int> queue = new();
        queue.Enqueue(start);

        List<Vector2Int> visited = new();
        visited.Add(start);

        Dictionary<Vector2Int, Vector2Int> previousPositions = new();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int currentPosition = queue.Dequeue();

            if (currentPosition == destination)
            {
                List<Vector2Int> path = new();
                Vector2Int position = destination;

                path.Add(position);

                while (position != start)
                {
                    position = previousPositions[position];
                    path.Add(position);
                }

                path.Reverse();
                return path;
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int nextPosition = currentPosition + direction;

                if (visited.Contains(nextPosition))
                    continue;

                if (!CanStep(gameManager, currentPosition, direction, nextPosition))
                    continue;

                queue.Enqueue(nextPosition);
                visited.Add(nextPosition);
                previousPositions.Add(nextPosition, currentPosition);
            }
        }

        return new List<Vector2Int>();
    }

    public static bool IsInRange(GameManager gameManager, Vector2Int pos, Vector2Int target, int maxDistance) 
    {
        List<Vector2Int> path = FindPath(gameManager, pos, target);

        if (path.Count != 0 && path.Count - 1 < maxDistance)
        {
            return true;
        }

        return false;
    }
}
