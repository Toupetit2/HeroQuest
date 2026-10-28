using System;
using UnityEngine;

// prefab d'un type de meuble, regle dans l'Inspector du QuestView
// le modele est oriente avec son grand cote le long de X (rotation 0)
[Serializable]
public class FurniturePrefab
{
    public FurnitureType type;
    public GameObject prefab;
}
