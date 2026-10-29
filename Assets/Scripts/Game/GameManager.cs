using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UIManager uiManager;

    [Header("Quête")]
    private int questNumber = 1;

    [Header("État de la partie")]
    private int currentTurn = 1;
    private bool isGameStarted = false;
    private GameState gameState;

    private List<HeroClass> turnOrder = new();

    private int currentPlayerIndex = 0;
    private bool isZargonTurn = false;

    [Header("État du tour")]    
    private bool hasMoveAction = true;
    private bool hasAction = true;

    private Tile selectedTile;
    private Monster selectedMonster;
    private Potion selectedPotion;
    private Equipment selectedEquipment;
    private Hero selectedHero;

    private MovementController movementController;
    private AttackController attackController;
    private PotionController potionController;
    private EquipmentController equipmentController;
    private LootController lootController;
    private BoardBuilder boardBuilder;
    private QuestLoader questLoader;

    private void Awake()
    {
        movementController = new MovementController(this);
        attackController = new AttackController(this);
        potionController = new PotionController(this);
        equipmentController = new EquipmentController(this);
        lootController = new LootController(this);

        //questLoader = new QuestLoader(gameState);
    }
    private void Start()
    {
        // Le generate map empeche la suite d'etre executé donc a fix

        boardBuilder.Generate();

        // Generate Map
        gameState.map = boardBuilder.map;


        

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
            isZargonTurn = true;
        }
        if (isZargonTurn) 
        {
            currentPlayerIndex = 0;
            currentTurn++;
            isZargonTurn = false;

            foreach (Monster monster in gameState.monsterList) 
            {
                monster.isSleeping = false;
                monster.isInTornado = false;
            }
        }
        StartTurn();
    }

    public bool IsZargonTurn()
    {
        return isZargonTurn;
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
    public bool HasAction()
    {
        return hasAction;
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
        lootController.PickUpLoot(GetCurrentHero(), GetCurrentHero().position);
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
        Monster monster = GetMonsterFromTile(tile);
        if (isZargonTurn && monster != null) // Si Zargon et il y a un monstre sur la case, set le monster plutot que la case
        {
            selectedMonster = monster;
        }
        selectedTile = tile;
        selectedHero = GetHeroFromTile(tile);
    }

    public Map GetMap() 
    {
        return gameState.map;
    }

    public Monster GetSelectedMonsterFromSelectedTile() 
    {
        Vector2Int selectedPos = new Vector2Int(selectedTile.x, selectedTile.y);

        foreach(Monster monster in gameState.monsterList) 
        {
            if (monster.position == selectedPos) 
            {
                return monster;
            }
        }
        return null;
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
    public void AttackConfirm(int customAttackDice = -1) 
    {
        Monster monster = GetMonsterFromTile(selectedTile);
        attackController.ConfirmAttack(monster, customAttackDice);
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

    [Button]
    public void PotionStart() 
    {
        potionController.StartPotionAction();
    }

    public void SetSelectedPotion(Potion potion) 
    {
        selectedPotion = potion;
    }

    [Button]
    public void PotionConfirm() 
    {
        potionController.UsePotionAction(selectedPotion);
    }

    [Button]
    public void EquipmentStart() 
    {
        equipmentController.StartEquipmentUse();
    }

    public void SetSelectedEquipment(Equipment equipment) 
    {
        selectedEquipment = equipment;
    }

    [Button]
    public void EquipmentConfirm() 
    {
        equipmentController.ConfirmEquipmentUse(selectedEquipment);
    }

    [Button]
    public void ZargonMoveMonster() 
    {
        movementController.MonsterConfirmMove(selectedMonster, selectedTile);
        Debug.Log($"Zargon déplace {selectedMonster.data.monsterClass.ToString()} en {selectedTile.x}, {selectedTile.y}");
    }

    private Hero GetHeroFromTile(Tile tile)
    {
        foreach (Hero hero in gameState.heroList)
        {
            if (hero.position == new Vector2Int(tile.x, tile.y))
            {
                return hero;
            }
        }
        return null;
    }

    [Button]
    public void ZargonAttackHero() 
    {
        attackController.ZargonConfirmAttack(selectedMonster, GetHeroFromTile(selectedTile));
        Debug.Log($"Zargon attaque {GetHeroFromTile(selectedTile).data.heroClass.ToString()} avec {selectedMonster.data.monsterClass.ToString()}");
    }

    public Tile GetSelectedTile() 
    {
        return selectedTile;
    }

    public Monster GetSelectedMonster() 
    {
        return selectedMonster;
    }

    public Potion GetSelectedPotion() 
    {
        return selectedPotion;
    }

    public Equipment GetSelectedEquipment() 
    {
        return selectedEquipment;
    }
    
    public Hero GetSelectedHero() 
    {
        return selectedHero;
    }

    public int GetCurrentTurn() 
    {
        return currentTurn;
    }
    
    public bool IsGameStarted() 
    {
        return isGameStarted;
    }

    public GameState GetGameState() 
    {
        return gameState;
    }

    public bool HasMoveAction() 
    {
        return hasMoveAction;
    }

    public void UseMoveAction() 
    {
        hasMoveAction = false;
    }

    public int GetQuestNumber() 
    {
        return questNumber;
    }

    public void DropLoot(Equipment equipment, Vector2Int position) 
    {
        lootController.DropLoot(equipment, position);
    }
}