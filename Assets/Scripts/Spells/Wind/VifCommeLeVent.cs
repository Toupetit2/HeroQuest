using UnityEngine;

[CreateAssetMenu(fileName = "VifCommeLeVent", menuName = "Scriptable Objects/VifCommeLeVent")]
public class VifCommeLeVent : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.selectedHero;

        user.movementDiceMultiplier = 2;
    }
}
