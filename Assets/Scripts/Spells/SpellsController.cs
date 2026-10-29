using UnityEngine;

public class SpellsController
{
    private GameManager gameManager;

    public SpellsController(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Init() 
    {
        
        foreach (Hero hero in gameManager.GetGameState().heroList) 
        {
            foreach (SpellType spellType in hero.spells) 
            {
                hero.spellList.Add()
            }
        }
    }

    private bool CanUseSpell(Hero hero, Spell spell) 
    {
        if (hero.)
    }
}