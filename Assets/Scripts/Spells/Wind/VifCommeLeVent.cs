using UnityEngine;

[CreateAssetMenu(fileName = "VifCommeLeVent", menuName = "Scriptable Objects/SpellsEffects/VifCommeLeVent")]
public class VifCommeLeVent : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.GetSelectedHero();

        user.movementDiceMultiplier = 2;
    }
}
