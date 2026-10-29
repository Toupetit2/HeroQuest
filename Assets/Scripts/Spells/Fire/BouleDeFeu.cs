using UnityEngine;

[CreateAssetMenu(fileName = "BouleDeFeu", menuName = "Scriptable Objects/BouleDeFeu")]
public class BouleDeFeu : CardEffect
{
    public override void Execute(GameManager gameManager) 
    {
        Monster target = gameManager.GetSelectedMonsterFromSelectedTile();

        target.SetHealth(target.GetHealth() - 2);

        int first = Dices.RollRedDice();
        int second = Dices.RollRedDice();

        if (first >= 5) 
        {
            target.SetHealth(target.GetHealth() + 1);
        }
        if (second >= 5)
        {
            target.SetHealth(target.GetHealth() + 1);
        }
    }
}
