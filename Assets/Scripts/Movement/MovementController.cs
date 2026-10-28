using UnityEngine;

public class MovementController
{
    private GameManager gameManager;
    private MovementSystem movementSystem;

    private MovementStates movementState;

    public MovementController(GameManager gameManager, Map map)
    {
        this.gameManager = gameManager;
        movementSystem = new MovementSystem(map);
    }

    public bool CanRoll()
    {
        if (movementState == MovementStates.None)
        {
            return gameManager.hasMoveAction;
        }
        return false;
    }

    public void RollDices()
    {
        movementSystem.RollMovement(gameManager.GetCurrentHero());
        movementState = MovementStates.ChoosingDestination;
    }

    public void ConfirmMove(Tile selectedTile)
    {
        if (movementState != MovementStates.ChoosingDestination || selectedTile == null)
            return;

        movementSystem.MoveHero(gameManager.GetCurrentHero(), selectedTile);

        selectedTile = null;
        movementState = MovementStates.None;
        gameManager.hasMoveAction = false;
    }
}