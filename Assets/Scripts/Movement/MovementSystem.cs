using System.Collections.Generic;
using UnityEngine;

public class MovementSystem
{
    private int movementPoints;
    private Map map;

    public MovementSystem(Map _map)
    {
        map = _map;
    }

    public int RollMovement(Hero hero)
    {
        movementPoints = Dices.RollRedDice(hero.data.movementDice);
        Debug.Log($"Total déplacement : {movementPoints}");
        return movementPoints;
    }

    public bool CanMoveTo(Hero hero, Tile destination) 
    {
        List<Vector2Int> path = FindPath(map, hero.position, new Vector2Int(destination.x, destination.y));

        if (path.Count != 0 && path.Count - 1 < movementPoints) 
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

    private bool CanStep(Vector2Int from, Vector2Int direction, Vector2Int to)
    {
        if (!map.InBounds(to.x, to.y))
            return false;

        if (map.HasWall(from.x, from.y, direction.x, direction.y))
            return false;

        if (!map.tiles[to.x, to.y].isWalkable())
            return false;

        return true;
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

                if (visited.Contains(nextPosition))
                    continue;

                if (!CanStep(currentPosition, direction, nextPosition))
                    continue;

                queue.Enqueue(nextPosition);
                visited.Add(nextPosition);
                previousPositions.Add(nextPosition, currentPosition);
            }
        }

        return new List<Vector2Int>();
    }

    public int GetMovementPoints() 
    {
        return movementPoints;
    }

    public List<Vector2Int> GetReachableTiles(Map map, Hero hero) 
    {
        Vector2Int start = hero.position;
        int maxCost = movementPoints;

        Queue<Vector2Int> queue = new();
        queue.Enqueue(start);

        
        Dictionary<Vector2Int, int> distances = new();
        distances[start] = 0;

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
            int currentDistance = distances[currentPosition];

            if (currentDistance >= maxCost)
                continue;

            foreach (Vector2Int direction in directions)
            {
                Vector2Int nextPosition = currentPosition + direction;

                if (distances.ContainsKey(nextPosition))
                    continue;

                if (!CanStep(currentPosition, direction, nextPosition))
                    continue;

                distances[nextPosition] = currentDistance + 1;
                queue.Enqueue(nextPosition);
            }
        }

        distances.Remove(start);

        return new List<Vector2Int>(distances.Keys);
    }



}
