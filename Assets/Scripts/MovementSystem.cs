using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VersionControl;
using UnityEngine;

public class MovementSystem
{
    private int movementPoints;
    private Map map;

    public MovementSystem(Map _map)
    {
        map = _map;
    }

    public void RollMovement(Hero hero)
    {
        movementPoints = Dices.RollRedDice(hero.data.movementDice);
    }

    public bool CanMoveTo(Hero hero, Tile destination) 
    {
        List<Vector2Int> path = FindPath(map, hero.position, new Vector2Int(destination.x, destination.y));

        if (path.Count != 0 && path.Count < movementPoints) 
        {
            return true;
        }

        return false;
    }

    public void MoveHero(Hero hero, Tile destination) 
    {
        if (!CanMoveTo(hero, destination)) 
        {
            return;
        }

        hero.position = new Vector2Int(destination.x, destination.y);
    }

    private List<Vector2Int> FindPath(Map map, Vector2Int start, Vector2Int destination)
    {
        Queue<Vector2Int> queue = new();
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

                if (!map.InBounds(nextPosition.x, nextPosition.y))
                    continue;

                if (visited.Contains(nextPosition))
                    continue;

                if (map.HasWall(currentPosition.x, currentPosition.y, direction.x, direction.y))
                    continue;

                if (!map.tiles[nextPosition.x, nextPosition.y].isWalkable)
                    continue;

                queue.Enqueue(nextPosition);
                visited.Add(nextPosition);
                previousPositions.Add(nextPosition, currentPosition);
            }
        }

        return new List<Vector2Int>();
    }

}
