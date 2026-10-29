
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

[CreateAssetMenu(fileName = "ATraversLaPierre", menuName = "Scriptable Objects/SpellsEffects/ATraversLaPierre")]
public class ATraversLaPierre : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.GetSelectedHero();

        user.isGhosted = true;
    }
}
