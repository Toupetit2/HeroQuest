using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Djinn", menuName = "Scriptable Objects/Djinn")]
public class Djinn : CardEffect
{
    private Door GetDoor(GameState gameState, Tile tile) 
    {
        foreach (Door door in gameState.doorList) 
        {
            if (door.x == tile.x && door.y == tile.y) 
            {
                return door;
            }
        }
        return null;
    }
    public override void Execute(GameManager gameManager) 
    {
        GameState gameState = gameManager.gameState;
        Door door = GetDoor(gameState, gameManager.selectedTile);
        if (door != null) // si case avec une porte
        {
            door.isOpen = true;
        }
        else 
        {
            gameManager.attackController.ConfirmAttack(gameManager.GetSelectedMonster(), 5);
        }
    }
}
