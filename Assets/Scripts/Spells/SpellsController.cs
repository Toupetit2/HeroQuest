using System.Collections.Generic;
using UnityEngine;

public class SpellsController
{
    private GameManager gameManager;
    private SpellsSystem spellsSystem;

    public SpellsController(GameManager gameManager)
    {
        this.gameManager = gameManager;
        spellsSystem = new SpellsSystem();
    }

    public void Init(Spell[] allSpells)
    {
        spellsSystem.Init(allSpells);

        foreach (Hero hero in gameManager.GetGameState().heroList)
        {
            hero.spellList.Clear();

            foreach (SpellType spellType in hero.spells)
                foreach (Spell spell in spellsSystem.GetSpells(spellType)) 
                {
                    hero.spellList.Add(spell);
                }
        }
    }

    private bool CanUseSpell(Hero hero, Spell spell) 
    {
        foreach (Spell heroSpell in hero.spellList) 
        {
            if (heroSpell == spell) 
            {
                return true;
            }
        }
        return false;
    }

    public void UseSpell(Hero hero, Spell spell) 
    {
        if (CanUseSpell(hero, spell)) 
        {
            spell.effect.Execute(gameManager);
            hero.spellList.Remove(spell);
        }
    }
}
