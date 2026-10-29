using UnityEngine;

[CreateAssetMenu(fileName = "Sommeil", menuName = "Scriptable Objects/Sommeil")]
public class Sommeil : CardEffect
{
    public override void Execute(GameManager gameManager)
    {
        Monster target = gameManager.GetSelectedMonsterFromSelectedTile();

        bool sixOnDice = false;

        if (target.data.monsterType != MonsterTypes.Undead) 
        {
            for (int i = 0; i < target.data.spirit;  i++) 
            {
                if (Dices.RollRedDice() == 6) 
                {
                    sixOnDice = true;
                }
            }
            if (!sixOnDice) 
            {
                target.isSleeping = true;
            }
        }
    }
}
