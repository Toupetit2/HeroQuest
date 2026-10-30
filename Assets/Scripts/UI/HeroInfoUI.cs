using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// fiche d'un heros sur l'ecran de choix des heros : un bouton "i" sur chaque heros ouvre sa carte
// les caracteristiques viennent de ses HeroData, la description est ecrite par nous (pas de texte officiel)
// ajoute par le UIManager sur le panneau de choix des heros, rien a placer dans la scene
public class HeroInfoUI : MonoBehaviour
{
    GameManager gameManager;
    Font font;
    GameObject cardPanel;
    Text titleText, roleText, statsText, magicText, equipmentText, descriptionText;

    // buttons : les boutons des heros, dans l'ordre de HeroClass (Barbarian, Dwarf, Elf, Wizard)
    public void Setup(GameManager manager, GameObject[] buttons)
    {
        gameManager = manager;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            HeroClass heroClass = (HeroClass)i;
            if (font == null)
            {
                Text existing = buttons[i].GetComponentInChildren<Text>(true);
                if (existing != null) font = existing.font;
            }
            AddInfoButton(buttons[i].transform, heroClass);
        }

        BuildCard();
    }

    // petite pastille doree "i" dans le coin en haut a droite du bouton du heros
    void AddInfoButton(Transform heroButton, HeroClass heroClass)
    {
        Button info = UIFactory.Button("InfoButton", heroButton, "i", 30, new Vector2(46f, 46f), Vector2.zero, () => Show(heroClass), font);
        RectTransform rt = (RectTransform)info.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-6f, -6f);
        info.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;
    }

    void BuildCard()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.rootCanvas.transform : transform;

        // fond sombre plein ecran : un clic a cote de la carte la ferme
        Image backdrop = UIFactory.Panel("HeroCard", root, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.65f), false);
        UIFactory.Stretch(backdrop.rectTransform);
        backdrop.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
        cardPanel = backdrop.gameObject;

        Transform card = UIFactory.Panel("Card", cardPanel.transform, new Vector2(680f, 640f), Vector2.zero, UIFactory.DarkBrown, true).transform;
        card.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;   // un clic sur la carte ne la ferme pas

        titleText = UIFactory.Label("Name", card, "", 50, UIFactory.Gold, TextAnchor.MiddleCenter, new Vector2(620f, 70f), new Vector2(0f, 260f), font);
        roleText = UIFactory.Label("Role", card, "", 24, UIFactory.Ivory, TextAnchor.MiddleCenter, new Vector2(620f, 36f), new Vector2(0f, 212f), font);
        roleText.fontStyle = FontStyle.Italic;
        UIFactory.Panel("Line", card, new Vector2(560f, 3f), new Vector2(0f, 186f), UIFactory.Gold, false);

        statsText = UIFactory.Label("Stats", card, "", 26, UIFactory.Ivory, TextAnchor.UpperLeft, new Vector2(560f, 200f), new Vector2(0f, 76f), font);
        statsText.lineSpacing = 1.15f;
        magicText = UIFactory.Label("Magic", card, "", 24, UIFactory.Gold, TextAnchor.MiddleLeft, new Vector2(560f, 36f), new Vector2(0f, -40f), font);
        equipmentText = UIFactory.Label("Equipment", card, "", 22, UIFactory.Ivory, TextAnchor.MiddleLeft, new Vector2(560f, 36f), new Vector2(0f, -78f), font);

        UIFactory.Panel("Line", card, new Vector2(560f, 3f), new Vector2(0f, -106f), UIFactory.Gold, false);
        descriptionText = UIFactory.Label("Description", card, "", 23, UIFactory.Ivory, TextAnchor.UpperLeft, new Vector2(560f, 130f), new Vector2(0f, -180f), font);

        UIFactory.Button("Close", card, "Close", 26, new Vector2(200f, 56f), new Vector2(0f, -280f), Hide, font);

        cardPanel.SetActive(false);
    }

    public void Show(HeroClass heroClass)
    {
        Hero hero = gameManager != null ? gameManager.GetHeroByClass(heroClass) : null;
        HeroData data = hero != null ? hero.data : null;

        // pastille de la couleur du heros, la meme que sur le plateau
        titleText.text = "<color=" + HeroColors.Hex(heroClass) + ">●</color> " + heroClass;
        roleText.text = Role(heroClass);
        descriptionText.text = Description(heroClass);

        if (data != null)
        {
            statsText.text =
                Stat("Attack", data.attackDices + " combat dice") +
                Stat("Defend", data.defenseDices + " combat dice") +
                Stat("Body points", data.health.ToString()) +
                Stat("Mind points", data.spirit.ToString()) +
                Stat("Movement", data.movementDice + " red dice");
            magicText.text = data.canUseSpells ? "Magic : " + (heroClass == HeroClass.Wizard ? "3 schools of spells" : "1 school of spells") : "Magic : none";
            equipmentText.text = "Starting equipment : " + Equipment(data);
        }
        else
        {
            statsText.text = "";
            magicText.text = "";
            equipmentText.text = "";
        }

        cardPanel.transform.SetAsLastSibling();   // au-dessus du reste de l'interface
        cardPanel.SetActive(true);
    }

    public void Hide()
    {
        cardPanel.SetActive(false);
    }

    static string Stat(string label, string value)
    {
        return "<color=#FFD24D>" + label + "</color>   " + value + "\n";
    }

    static string Equipment(HeroData data)
    {
        string weapon = data.baseWeapon != null && !string.IsNullOrEmpty(data.baseWeapon.equipmentName) ? English(data.baseWeapon.equipmentName) : "";
        string armor = data.baseArmor != null && !string.IsNullOrEmpty(data.baseArmor.equipmentName) ? English(data.baseArmor.equipmentName) : "";
        if (weapon == "" && armor == "") return "none";
        if (armor == "") return weapon;
        if (weapon == "") return armor;
        return weapon + ", " + armor;
    }

    // les noms d'equipement des donnees sont en francais : traduits seulement pour l'affichage (les donnees ne changent pas)
    static readonly Dictionary<string, string> EquipmentNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Epée barbare", "Broadsword" }, { "Epée courte", "Shortsword" }, { "Epée longue", "Longsword" },
        { "Hache de combat", "Battle Axe" }, { "Hachette", "Hand Axe" }, { "Poignard", "Dagger" },
        { "Arbalète", "Crossbow" }, { "Bâton", "Staff" }, { "Eau bénite", "Holy Water" },
        { "Armure de plaques", "Plate Armor" }, { "Cotte de mailles", "Chain Mail" }, { "Casque", "Helmet" },
        { "Bouclier", "Shield" }, { "Bracelets", "Bracers" }
    };

    static string English(string name)
    {
        string english;
        return EquipmentNames.TryGetValue(name.Trim(), out english) ? english : name;
    }

    static string Role(HeroClass heroClass)
    {
        switch (heroClass)
        {
            case HeroClass.Barbarian: return "The warrior";
            case HeroClass.Dwarf: return "The trap expert";
            case HeroClass.Elf: return "The warrior-mage";
            default: return "The spellcaster";
        }
    }

    // textes ecrits par nous, a partir des regles (pas de texte officiel)
    static string Description(HeroClass heroClass)
    {
        switch (heroClass)
        {
            case HeroClass.Barbarian:
                return "The strongest fighter of the group. He hits hard and can take a lot of blows, but he cannot use magic. Keep him in the front line.";
            case HeroClass.Dwarf:
                return "A tough and steady fighter. He is the only hero who can disarm traps without a tool kit, which makes him precious in dangerous corridors. No magic.";
            case HeroClass.Elf:
                return "A good fighter who also masters one school of spells. A versatile hero, at ease both in close combat and with magic.";
            default:
                return "The weakest in combat but the master of magic: he knows three schools of spells. Keep him behind the others and let his spells do the work.";
        }
    }
}
