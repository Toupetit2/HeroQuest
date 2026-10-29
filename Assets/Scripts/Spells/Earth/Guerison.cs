using UnityEngine;

[CreateAssetMenu(fileName = "Guerison", menuName = "Scriptable Objects/SpellsEffects/Guerison")]
public class Guerison : CardEffect
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
