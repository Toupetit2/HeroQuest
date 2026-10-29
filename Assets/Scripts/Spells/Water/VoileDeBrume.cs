using UnityEngine;

[CreateAssetMenu(fileName = "VoileDeBrume", menuName = "Scriptable Objects/SpellsEffects/VoileDeBrume")]
public class VoileDeBrume : CardEffect
{
    public override void Execute(GameManager gameManager)
    {

        Hero user = gameManager.GetSelectedHero();

        user.isInvisible = true;
    }
}
