using System.Collections.Generic;
using UnityEngine;

public class SpellsSystem
{
    private GameManager gameManager;
    private GameState gameState;

    public SpellsSystem(GameManager gameManager)
    {
        this.gameManager = gameManager;
        this.gameState = gameManager.GetGameState();
    }

    public List<Spell> GetSpells(SpellType spellType) 
    {
        List<Spell> spell = new List<Spell>();
         test;



    }
}
