using UnityEngine;

[CreateAssetMenu(fileName = "EauBenite", menuName = "Scriptable Objects/EauBenite")]
public class EauBenite : CardEffect
{
    public override void Execute(GameManager gameManager) 
    {
        Monster target = gameManager.GetSelectedMonsterFromSelectedTile();

        if (target != null) 
        {
            if (target.data.monsterType == MonsterTypes.Undead) 
            {
                target.SetHealth(0);
            }
        }
    }
}
