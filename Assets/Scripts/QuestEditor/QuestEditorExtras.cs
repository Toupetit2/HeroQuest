using System;

// champs que seul l'editeur de quetes utilise.
// ils sont dans une classe a part (qui herite de QuestDefinition) pour ne pas modifier QuestDefinition.cs :
// le QuestLoader lit le meme JSON avec QuestDefinition et ignore simplement ces champs.

// une note du livre de quetes : lettre affichee sur la carte + texte reserve a Zargon
// une meme lettre peut apparaitre plusieurs fois sur la carte, le texte n'est alors ecrit qu'une fois
[Serializable]
public class NoteEntry
{
    public string letter;  // "A", "B"... ou vide pour une note generale
    public string text;
    public int x = -1;     // case de la lettre sur la carte, -1 = pas sur la carte
    public int y = -1;
}

[Serializable]
public class EditorQuestDefinition : QuestDefinition
{
    public string parchment;     // texte lu a voix haute aux joueurs (recopie aussi dans "intro", lu par le jeu)
    public string zargonNote;    // consigne secrete pour Zargon
    public NoteEntry[] notes;
}
