using UnityEngine;
using System.Collections.Generic;

public class MovementSystem
{
    private int movementPoints;
    private Map map;
    private GameManager gameManager;

    public MovementSystem(GameManager _gameManager)
    {
        gameManager = _gameManager;
        map = _gameManager.GetMap();
    }

    public int RollMovement(Hero hero)
    {
        movementPoints = Dices.RollRedDice(hero.data.movementDice * hero.movementDiceMultiplier);
        hero.movementDiceMultiplier = 1;
        Debug.Log($"Total déplacement : {movementPoints}");
        return movementPoints;
    }

    public bool CanMoveTo(Hero hero, Tile destination) 
    {
        return Pathfinding.IsInRange(gameManager, hero.position, new Vector2Int(destination.x, destination.y), movementPoints);
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

                if (!Pathfinding.CanStep(gameManager, currentPosition, direction, nextPosition))
                    continue;

                distances[nextPosition] = currentDistance + 1;
                queue.Enqueue(nextPosition);
            }
        }

        distances.Remove(start);

        return new List<Vector2Int>(distances.Keys);
    }

    public bool MonsterCanMoveTo(Monster monster, Tile destination)
    {
        return Pathfinding.IsInRange(gameManager, monster.position, new Vector2Int(destination.x, destination.y), monster.data.movementCases);
    }

    public void MoveMonster(Monster monster, Tile destination) 
    {
        if (!MonsterCanMoveTo(monster, destination))
        {
            return;
        }

        monster.position = new Vector2Int(destination.x, destination.y);
    }

}
