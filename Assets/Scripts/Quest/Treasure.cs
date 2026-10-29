using System.Collections.Generic;

// tresor special d'une salle (notes du livre de quetes)
// d'apres les regles, seul le premier heros qui fouille la salle le trouve
// un tresor sans or ni objet = rien a trouver (on ne tire pas de carte tresor)
public class Treasure
{
    public int x;               // case du meuble ou de la lettre sur la carte
    public int y;
    public string room;         // salle de cette case
    public int gold;
    public string item;         // objet trouve, vide = aucun
    public bool isTrapped;      // piege : se declenche si on fouille sans desarmer
    public int trapDamage;      // points de Corps perdus quand le piege se declenche
    public List<string> requiredKills = new List<string>(); // ids des monstres a tuer avant (quete 10)
    public bool isFound;
    public bool isDisarmed;
}
