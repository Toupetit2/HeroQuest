using UnityEngine;

[CreateAssetMenu(fileName = "PeauDeRoc", menuName = "Scriptable Objects/PeauDeRoc")]
public class PeauDeRoc : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.selectedHero;

        user.bonusDefenseDice = 1;
    }
}
