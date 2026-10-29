using UnityEngine;

[CreateAssetMenu(fileName = "VoileDeBrume", menuName = "Scriptable Objects/VoileDeBrume")]
public class VoileDeBrume : CardEffect
{
    public override void Execute(GameManager gameManager)
    {

        Hero user = gameManager.GetSelectedHero();

        user.isInvisible = true;
    }
}
