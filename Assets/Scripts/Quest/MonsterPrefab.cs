using System;
using UnityEngine;

// prefab d'un type de monstre, regle dans l'Inspector du QuestView
[Serializable]
public class MonsterPrefab
{
    public MonsterClass type;
    public GameObject prefab;
}
