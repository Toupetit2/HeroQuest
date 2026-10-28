using System.Collections.Generic;
using UnityEngine;

public class Search
{
    private static List<Tile> GetTilesFromZone(Map map, string zone) 
    {
        List<Tile> tiles = new List<Tile>();

        foreach (Tile tile in map.tiles) 
        {
            if (tile.zone == zone) 
            {
                tiles.Add(tile);
            }
        }
        return tiles;
    }

    private static List<Tile> GetVisibleTilesFromPosition(Map map, Vector2Int pos) 
    {
        List<Tile> tiles = new List<Tile>();

        foreach (Tile tile in map.tiles)
        {
            if (Raycast.CanSee(map, pos, new Vector2Int(tile.x, tile.y)))
            {
                tiles.Add(tile);
            }
        }
        return tiles;
    }

    public static void DetectTrap(Map map, Hero hero) 
    {
        int x = hero.position.x;
        int y = hero.position.y;

        string zone = map.Get(x, y).zone;

        List<Tile> zoneTiles;

        if (zone == ".") 
        {
            zoneTiles = GetVisibleTilesFromPosition(map, hero.position);
        }
        else 
        {
            zoneTiles = GetTilesFromZone(map, zone);
        }

        foreach (Tile tile in zoneTiles) 
        {
            if (tile.trap != null) 
            {
                tile.trap.state = TrapState.Decouvert;
            }
        }
    }

    private static List<Tile> GetAdjacentTilesFromTileList(Map map, List<Tile> tiles) 
    {
        List<Tile> newTiles = new List<Tile>();
        List<Vector2Int> sides = new List<Vector2Int>();

        sides.Add(new Vector2Int(1, 0));
        sides.Add(new Vector2Int(-1, 0));
        sides.Add(new Vector2Int(0, -1));
        sides.Add(new Vector2Int(0, 1));

        foreach (Tile tile in tiles) 
        {
            foreach (Vector2Int side in sides) 
            {
                Tile newTile = null;
                Vector2Int pos = new Vector2Int(tile.x + side.x, tile.y + side.y);
                if (map.InBounds(pos.x, pos.y)) 
                {
                    newTile = map.Get(pos.x, pos.y);

                    if (!newTiles.Contains(tile))
                    {
                        newTiles.Add(tile);
                    }
                }   
            }
        }
        return newTiles;
    }

    public static void DetectSecretDoor(GameState gameState, Hero hero) 
    {
        int x = hero.position.x;
        int y = hero.position.y;

        Map map = gameState.map;

        string zone = map.Get(x, y).zone;

        List<Tile> zoneTiles;

        if (zone == ".")
        {
            zoneTiles = GetVisibleTilesFromPosition(map, hero.position);
        }
        else
        {
            zoneTiles = GetTilesFromZone(map, zone);
        }

        zoneTiles = GetAdjacentTilesFromTileList(map, zoneTiles);

        foreach (Tile tile in zoneTiles) 
        {
            foreach (Door door in gameState.doorList)
            {

                if (door.isSecret)
                {
                    if (door.x == tile.x && door.y == tile.y) 
                    {
                        door.isFound = true;
                    }
                }
            }
        }        
    }
}
