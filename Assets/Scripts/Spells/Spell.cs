using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Spell", menuName = "Scriptable Objects/Spell")]
public class Spell : ScriptableObject
{
    public string spellName;
    public string description;
    public Sprite illustration;
    public SpellType spellType;
    public CardEffect effect;
}
