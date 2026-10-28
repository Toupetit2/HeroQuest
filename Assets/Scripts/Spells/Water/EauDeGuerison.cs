using UnityEngine;

public class EauDeGuerison : CardEffect
{
    public override void Execute()
    {
        return;

        Hero user; // choose hero UI

        user.currentHealth += 4;

        if (user.currentHealth > user.data.health) 
        {
            user.currentHealth = user.data.health;
        }
        
    }
}
