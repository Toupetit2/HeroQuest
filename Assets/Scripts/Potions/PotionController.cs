using UnityEngine;

public class PotionController
{
    private GameManager gameManager;

    public PotionController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public PotionsStates potionsState;

    public void StartPotionAction() 
    {
        potionsState = PotionsStates.ChoosingPotion;
    }

    public void UsePotionAction() 
    {
        potionsState = PotionsStates.UsingPotion;

        gameManager.GetCurrentHero()
            {
            
        }
    }
}
