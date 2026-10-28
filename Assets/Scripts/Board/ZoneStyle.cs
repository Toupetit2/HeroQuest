using System;
using UnityEngine;

// sol d'une zone, regle dans l'Inspector du BoardBuilder
[Serializable]
public class ZoneStyle
{
    public string zone;            // "." = couloir, "S1".."S22" = salle
    public GameObject floorPrefab; // vide = prefab de sol par defaut
    public Material material;      // vide = couleurs du prefab, ou materiau par defaut
}
