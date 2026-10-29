using UnityEngine;

[CreateAssetMenu(fileName = "LeFeuDuCourroux", menuName = "Scriptable Objects/LeFeuDuCourroux")]
public class LeFeuDuCourroux : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Monster target = gameManager.GetSelectedMonsterFromSelectedTile();

        if (Dices.RollRedDice() < 5)
            target.SetHealth(target.GetHealth() - 1);
    }
}
