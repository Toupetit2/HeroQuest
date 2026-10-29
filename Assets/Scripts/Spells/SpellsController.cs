using System;
using UnityEngine;

// Compatible avec GameManager.SpellAction(Spell) et les effets
// de Spell.zip qui exposent Execute(GameManager gameManager).
public class SpellsController
{
    private readonly GameManager gameManager;
    private Spell[] allSpells = Array.Empty<Spell>();

    public SpellsController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Init(Spell[] spells)
    {
        allSpells = spells ?? Array.Empty<Spell>();
    }

    public void UseSpell(Hero hero, Spell spell)
    {
        if (gameManager == null || hero == null || spell == null)
        {
            Debug.LogWarning("Sort impossible : GameManager, heros ou sort manquant.");
            return;
        }

        if (Array.IndexOf(allSpells, spell) < 0)
        {
            Debug.LogWarning("Sort absent de GameManager > All Spells : " + spell.spellName);
            return;
        }

        if (hero.spells == null || !hero.spells.Contains(spell.spellType))
        {
            Debug.LogWarning("Ce heros ne possede pas l'element du sort : " + spell.spellName);
            return;
        }

        if (spell.effect == null)
        {
            Debug.LogWarning("Effet manquant pour le sort : " + spell.spellName);
            return;
        }

        if (!gameManager.HasAction())
        {
            Debug.LogWarning("Ce heros a deja utilise son action.");
            return;
        }

        try
        {
            // Les effets fournis dans Spell.zip attendent le GameManager.
            spell.effect.Execute(gameManager);
            gameManager.UseAction();
        }
        catch (Exception ex)
        {
            // N'utilise pas l'action si l'effet leve une exception.
            Debug.LogException(ex);
        }
    }
}
