using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Equipment", menuName = "Scriptable Objects/Equipment")]
public class Equipment : ScriptableObject
{
    public string equipmentName;
    public string description;
    public Sprite illustration;
    public int price;
    public CardEffect effect;
    public List<HeroClass> bannedHeros;
    public List<Equipment> bannedEquipments;
    public bool singleUse;
}
