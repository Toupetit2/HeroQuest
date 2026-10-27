using UnityEngine;

public class AttackController
{
    private GameManager gameManager;
    private AttackSystem attackSystem;
    private AttackStates attackState = AttackStates.None;
    private Monster selectedTarget;

    public AttackController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public bool CanAttack() 
    {
        if (attackState == AttackStates.None && gameManager.hasAction) 
        {
            return true;
        }
        return false;
    }

    public void StartAttack() 
    {
        attackState = AttackStates.ChoosingTarget;
    }

    public void SelectTarget(Monster monster) 
    {
        selectedTarget = monster;
    }

    public void ConfirmAttack() 
    {
        if (selectedTarget != null) 
        {
            attackSystem.ResolveAttack(gameManager.GetCurrentHero(), selectedTarget);
            selectedTarget = null;
            attackState = AttackStates.None;
            gameManager.hasAction = false;
        }

    }
}
