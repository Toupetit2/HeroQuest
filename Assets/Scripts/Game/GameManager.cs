using NUnit.Framework.Constraints;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Quête")]
    public int questNumber = 1;

    [Header("État de la partie")]
    public int currentTurn = 1;
    public bool isGameStarted = false;
    public GameState gameState;

    public List<HeroClass> turnOrder;

    public int currentPlayerIndex = 0;

    [Header("État du tour")]    
    public bool hasMoveAction = true;
    public bool hasAction = true;

    [Button]
    public void Test() 
    {
        return;
    }

    private void Start()
    {
        // Generate Map
        gameState.map = new Map(
                ". . . .\n" +
                ". . . .\n" +
                ". . . ."
            );

        // Show Intro UI -> Hero choice UI -> Spell choice UI
    }

    public void StartGame()
    {
        isGameStarted = true;
        StartTurn();
    }

    public void StartTurn() 
    {
        hasMoveAction = true;
        hasAction = true;
    }

    public void EndTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= gameState.heroList.Count)
        {
            currentPlayerIndex = 0;
            currentTurn++;
        }
        StartTurn();
    }

    public void AddHeroTurn(HeroClass hero) 
    {
        turnOrder.Add(hero);
    }

    public void GiveSpellType(SpellType type) 
    {
        GetHero(turnOrder[currentPlayerIndex]).spells.Add(type);
    }

    public void MoveAction() 
    {
        hasMoveAction = false;
        // Move
    }

    public void AttackAction() 
    {
        hasAction = false;
        // Attack
    }

    public void SpellAction() 
    {
        
    }

    public void SearchTreasureAction() 
    {
    
    }

    public void SearchSecretDoorAction() 
    {
        
    }

    public void DetectTrapAction() 
    {
    
    }
    
    public void DisableTrapAction() 
    {
    
    }
    
    public void OpenDoorFreeAction() 
    {
        
    }

    public void GiveItemFreeAction() 
    {
        
    }

    public void GetLootFreeAction() 
    {
        
    }

    private Hero GetHero(HeroClass hero) 
    {
        for (int i = 0; i < gameState.heroList.Count; i++) 
        {
            if (gameState.heroList[i].data.heroClass == hero) 
            {
                return gameState.heroList[i];
            }
        }
        return null;
    }

    public void ChangeQuest(int questNb) 
    {
        questNumber = questNb;
        //map id
    }
}

