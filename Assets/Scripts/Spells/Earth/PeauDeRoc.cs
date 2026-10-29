using UnityEngine;

[CreateAssetMenu(fileName = "PeauDeRoc", menuName = "Scriptable Objects/SpellsEffects/PeauDeRoc")]
public class PeauDeRoc : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.GetSelectedHero();

        user.bonusDefenseDice = 1;
    }
}
