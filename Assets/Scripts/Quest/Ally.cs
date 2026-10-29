using UnityEngine;

// personnage a escorter (ex : Sir Ragnar, quete 2), ni heros ni monstre
public class Ally
{
    public string id;
    public Vector2Int position;
    public int attack;
    public int defense;
    public int body;
    public int currentHealth;
    public bool isFound;
}
