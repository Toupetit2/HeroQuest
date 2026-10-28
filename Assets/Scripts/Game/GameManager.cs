using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UIManager uiManager;

    [Header("Quête")]
    public int questNumber = 1;

    [Header("État de la partie")]
    public int currentTurn = 1;
    public bool isGameStarted = false;
    public GameState gameState;

    public List<HeroClass> turnOrder = new();

    public int currentPlayerIndex = 0;

    [Header("État du tour")]    
    public bool hasMoveAction = true;
    public bool hasAction = true;

    public Tile selectedTile;

    public MovementController movementController;
    public AttackController attackController;

    [Button]
    public void Test() 
    {
        return;
    }

    private void Start()
    {
        // Le generate map empeche la suite d'etre executé donc a fix
        
        // Generate Map
        gameState.map = new Map(
                ". . . .\n" +
                ". . . .\n" +
                ". . . ."
            );

        movementController = new MovementController(this, gameState.map);
        attackController = new AttackController(this);

        // Show Intro UI
        //

        // Hero Choice
        //uiManager.ShowHeroChoice(true);

        // Spell Choice
        //uiManager.ShowSpellChoice(true);

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

    public bool UseAction() 
    {
        if (hasAction)
        {
            hasAction = false;
            return true;
        }
        return false;
    }

    public void SpellAction() 
    {
        
    }

    public void SearchTreasureAction() // une fois par joueur par pièce
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

    public Hero GetCurrentHero() 
    {
        foreach (Hero h in gameState.heroList) 
        {
            if (h.data.heroClass == turnOrder[currentPlayerIndex]) 
            {
                return h;
            }
        }
        return null;
    }

    public void ChangeQuest(int questNb) 
    {
        questNumber = questNb;
        //map id
    }

    public void SetSelectedTile(Tile tile)
    {
        selectedTile = tile;
    }

    public Map GetMap() 
    {
        return gameState.map;
    }


    // Deplacement
    [Button]
    public void MovementRollDices()
    {
        if (movementController.CanRoll())
            movementController.RollDices();
    }

    [Button]
    public void MovementConfirmMove() 
    {
        movementController.ConfirmMove(selectedTile);
    }

    // Attaque
    [Button]
    public void AttackStart() 
    {
        if (attackController.CanAttack()) 
        {
            attackController.StartAttack();
        }
    }

    private Monster GetMonsterFromTile(Tile tile) 
    {
        foreach (Monster monster in gameState.monsterList) 
        {
            if (monster.position == new Vector2Int(tile.x, tile.y)) 
            {
                return monster;
            }
        }
        return null;
    }

    [Button]
    public void AttackConfirm() 
    {
        Monster monster = GetMonsterFromTile(selectedTile);
        attackController.ConfirmAttack(monster);
    }

    [Button]
    public void SearchSecretDoorAction()
    {
        if (UseAction())
        {
            Search.DetectSecretDoor(gameState, GetCurrentHero());
        }
    }

    [Button]
    public void DetectTrapAction()
    {
        if (UseAction())
        {
            Search.DetectTrap(gameState.map, GetCurrentHero());
        }
    }

    
    
}

