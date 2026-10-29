using UnityEngine;

[CreateAssetMenu(fileName = "Courage", menuName = "Scriptable Objects/Courage")]
public class Courage : CardEffect
{
    public override void Execute(GameManager gameManager) 
    {
        Hero user = gameManager.selectedHero;

        user.bonusAttackDice += 2;
    }
}
