using UnityEngine;

// couleur de chaque heros, partagee par le plateau (prefab teinte) et l'interface (icone du tour)
public static class HeroColors
{
    public static Color Of(HeroClass heroClass)
    {
        switch (heroClass)
        {
            case HeroClass.Barbarian: return new Color(0.85f, 0.20f, 0.15f);   // rouge
            case HeroClass.Dwarf: return new Color(0.95f, 0.65f, 0.10f);       // orange
            case HeroClass.Elf: return new Color(0.25f, 0.75f, 0.30f);         // vert
            default: return new Color(0.25f, 0.45f, 0.95f);                     // bleu : Wizard
        }
    }

    // pour le texte riche : "#D9332A"
    public static string Hex(HeroClass heroClass)
    {
        return "#" + ColorUtility.ToHtmlStringRGB(Of(heroClass));
    }
}
