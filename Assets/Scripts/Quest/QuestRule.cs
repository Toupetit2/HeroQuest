// regles propres a une quete : le JSON les active, le gameplay les applique
// une valeur par regle, pas de texte libre
public enum QuestRule
{
    SauvetageRagnar,    // Q2 : Ragnar trouve = alarme, toutes les portes s'ouvrent ; pas de fouille dans la cellule
    CoffresLourds,      // Q4 : un heros porte un seul coffre et ne lance qu'1 de rouge pour bouger
    SansEquipement,     // Q6 : pas d'equipement ni de sort avant d'avoir fouille la salle A ; pas de fouille dans la cellule
    Teleportation,      // Q10 : passer une porte = stop, 2 des rouges, teleportation sur le repere du meme numero
    OrDeLaMine,         // Q10 : 5000 or a la mine, sans attaque ni defense en le portant, sans valeur a la fin
    EboulementDerriere, // Q12 : le bloc qui tombe se declenche seul apres le passage du dernier heros
    BlocsSansBlocage    // Q13 : les blocs ne bloquent pas ; 1 de rouge, 4-6 = -1 Corps (casque : 6 seulement)
}
