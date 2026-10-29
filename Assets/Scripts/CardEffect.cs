using UnityEngine;

[CreateAssetMenu(fileName = "CardEffect", menuName = "Scriptable Objects/CardEffect")]
public abstract class CardEffect : ScriptableObject
{
    public abstract void Execute(GameManager gameManager);
}
