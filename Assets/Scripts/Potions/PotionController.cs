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

    public void UsePotionAction(Potion potion) 
    {
        potionsState = PotionsStates.UsingPotion;

        if (potion != null) 
        {
            Hero hero = gameManager.GetCurrentHero();

            if (potion.healingDices > 0) 
            {
                int totalHeal = Dices.RollRedDice(potion.healingDices);

                hero.SetHealth(hero.GetHealth() + totalHeal);
            }

            hero.bonusAttackDice += potion.bonusAttackDices;

            if (potion.attackTwice) 
            {
                hero.canAttackTwice = true;
            }
        }
    }
}
