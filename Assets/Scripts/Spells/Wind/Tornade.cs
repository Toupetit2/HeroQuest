using UnityEngine;

[CreateAssetMenu(fileName = "Tornade", menuName = "Scriptable Objects/Tornade")]
public class Tornade : CardEffect
{
    public override void Execute(GameManager gameManager) 
    {
        gameManager.GetSelectedMonsterFromSelectedTile().isInTornado = true;
    }
}
