public enum ObjectiveType
{
    Tuer,           // tuer un monstre precis (target = son id)
    TuerTous,       // tuer tous les monstres de la carte
    TrouverObjet,   // trouver un objet en fouillant une salle (target = objet, room = salle)
    Escorter,       // amener un personnage vivant a l'escalier (target = personnage)
    Rapporter,      // rapporter des objets a l'escalier (target = objet, count = nombre)
    Escalier        // revenir a l'escalier
}
