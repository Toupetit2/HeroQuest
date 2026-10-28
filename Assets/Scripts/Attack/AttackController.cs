using System.Collections.Generic;
using UnityEngine;

public class AttackController
{
    private GameManager gameManager;
    private AttackSystem attackSystem;
    private AttackStates attackState = AttackStates.None;

    public AttackController(GameManager gameManager)
    {
        this.gameManager = gameManager;
        attackSystem = new AttackSystem(gameManager.gameState);
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

    public void ConfirmAttack(Monster target) 
    {
        if (target != null) 
        {
            List<Monster> inRange = attackSystem.GetTargetInRange(gameManager.GetCurrentHero());

            foreach (Monster monster in inRange) 
            {
                if (monster == target) 
                {
                    attackSystem.ResolveAttack(gameManager.GetCurrentHero(), target);
                    attackState = AttackStates.None;
                    gameManager.hasAction = false;
                    return;
                }
            }
            Debug.Log("The target is not in range (AttackController.ConfirmAttack())");
            return;
        }
        Debug.Log("No valid target selected (AttackController.ConfirmAttack())");
    }
}
