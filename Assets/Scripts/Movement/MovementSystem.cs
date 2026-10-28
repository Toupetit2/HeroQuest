using UnityEngine;
using System.Collections.Generic;

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
        return Pathfinding.IsInRange(map, hero.position, new Vector2Int(destination.x, destination.y), 999);
    }

    public void MoveHero(Hero hero, Tile destination) 
    {
        if (!CanMoveTo(hero, destination)) 
        {
            return;
        }

        hero.position = new Vector2Int(destination.x, destination.y);
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

                if (!Pathfinding.CanStep(map, currentPosition, direction, nextPosition))
                    continue;

                distances[nextPosition] = currentDistance + 1;
                queue.Enqueue(nextPosition);
            }
        }

        distances.Remove(start);

        return new List<Vector2Int>(distances.Keys);
    }



}
