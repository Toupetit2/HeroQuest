using UnityEngine;

[CreateAssetMenu(fileName = "EauDeGuerison", menuName = "Scriptable Objects/SpellsEffects/EauDeGuerison")]
public class EauDeGuerison : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.GetSelectedHero();

        user.currentHealth += 4;

        if (user.currentHealth > user.data.health) 
        {
            user.currentHealth = user.data.health;
        }
        
    }
}
