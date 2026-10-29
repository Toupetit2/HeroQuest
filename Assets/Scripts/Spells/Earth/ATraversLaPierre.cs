
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

[CreateAssetMenu(fileName = "ATraversLaPierre", menuName = "Scriptable Objects/ATraversLaPierre")]
public class ATraversLaPierre : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Hero user = gameManager.selectedHero;

        user.isGhosted = true;
    }
}
