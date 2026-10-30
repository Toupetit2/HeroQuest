using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public UIManager uiManager;

    [Header("Quête")]
    private int questNumber = 1;

    [Header("État de la partie")]
    private int currentTurn = 1;
    private bool isGameStarted = false;
    private GameState gameState = new GameState();

    private List<HeroClass> turnOrder = new();

    private int currentHeroIndex = 0;
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
    private SpellsController spellsController;
    private MoveHero moveHero;
    
    [SerializeField] private BoardBuilder boardBuilder;   // celui de la scene, a glisser dans l'Inspector
    [SerializeField] private QuestView questView;       // celui de la scene, a glisser dans l'Inspector
    private QuestLoader questLoader;

    [Header("Hero")]
    [SerializeField] private List<HeroData> heroInitList;
    [SerializeField] private Object heroPrefab;
    

    [Header("Spells")]
    [SerializeField] private Spell[] allSpells;

    public List<Player> players = new List<Player>();

    private void Awake()
    {
        movementController = new MovementController(this);
        attackController = new AttackController(this);
        potionController = new PotionController(this);
        equipmentController = new EquipmentController(this);
        lootController = new LootController(this);
        spellsController = new SpellsController(this);
        moveHero = new MoveHero(heroPrefab);
    }

    private void Start()
    {
        // le plateau d'abord : le QuestLoader a besoin de la map
        boardBuilder.Generate();
        gameState.map = boardBuilder.map;

        questLoader = new QuestLoader(gameState);
        if (QuestSelection.HasSelection)
        {
            // Campagne ou Mes quetes : le menu fournit le JSON selectionne.
            // Ne JAMAIS retomber silencieusement sur la quete 1 si le fichier manque.
            string chosenJson;
            try
            {
                chosenJson = QuestSelection.ReadJson();
            }
            catch (System.Exception e)
            {
                Debug.LogError("HeroQuest : impossible de lire la quete " + QuestSelection.FileName + " : " + e);
                enabled = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(chosenJson))
            {
                Debug.LogError("HeroQuest : quete introuvable ou vide : " + QuestSelection.FileName +
                    ". Verifie qu'elle a bien ete enregistree dans Mes quetes.");
                enabled = false;
                return;
            }

            try
            {
                questLoader.Load(chosenJson);
                Debug.Log("HeroQuest : chargement de " + QuestSelection.FileName +
                    (QuestSelection.IsCampaign ? " (campagne)" : " (Mes quetes)"));
            }
            catch (System.Exception e)
            {
                Debug.LogError("HeroQuest : quete invalide " + QuestSelection.FileName + " : " + e);
                enabled = false;
                return;
            }
        }
        else
        {
            // Lancement direct de la scene de jeu pour les tests : quete officielle.
            questLoader.LoadQuest(questNumber);
        }
        questView.Show(gameState);

        movementController = new MovementController(this);
        attackController = new AttackController(this);
        potionController = new PotionController(this);
        equipmentController = new EquipmentController(this);
        lootController = new LootController(this);

        foreach (HeroData hero in heroInitList) 
        {
            gameState.heroList.Add(new Hero(hero));
        }

    }
    [Button]
    public void StartGame()
    {
        uiManager.ShowGameUI();
        isGameStarted = true;
        spellsController.Init(allSpells);
        moveHero.Init(gameState.heroList);
        StartTurn();
    }

    public void StartTurn() 
    {
        hasMoveAction = true;
        hasAction = true;
        uiManager.SetTurnText();
    }

    public void EndTurn()
    {
        currentHeroIndex++;
        SkipDeadHeroes();

        if (currentHeroIndex >= turnOrder.Count)
        {
            currentHeroIndex = 0;
            isZargonTurn = true;
        }
        if (isZargonTurn)
        {
            currentHeroIndex = 0;
            currentTurn++;
            isZargonTurn = false;

            foreach (Monster monster in gameState.monsterList)
            {
                monster.isSleeping = false;
                monster.isInTornado = false;
            }
            SkipDeadHeroes();
        }
        StartTurn();
    }

    // les heros morts ne jouent plus
    private void SkipDeadHeroes()
    {
        while (currentHeroIndex < turnOrder.Count)
        {
            Hero hero = GetHeroByClass(turnOrder[currentHeroIndex]);
            if (hero == null || !hero.isDead) return;
            currentHeroIndex++;
        }
    }

    // retire du plateau les monstres et les heros a 0 PV ou moins, puis verifie victoire et defaite
    // a appeler apres tout ce qui peut faire perdre des PV (attaque, sort, objet)
    public void CheckDeaths()
    {
        for (int i = gameState.monsterList.Count - 1; i >= 0; i--)
        {
            Monster monster = gameState.monsterList[i];
            if (monster.currentHealth <= 0) KillMonster(monster);
        }

        foreach (Hero hero in gameState.heroList)
        {
            if (!hero.isDead && hero.currentHealth <= 0) KillHero(hero);
        }

        UpdateGameStatus();
    }

    private void KillMonster(Monster monster)
    {
        gameState.monsterList.Remove(monster);
        questView.RemoveMonster(monster);
        if (selectedMonster == monster) selectedMonster = null;

        if (!string.IsNullOrEmpty(monster.id)) gameState.killedMonsterIds.Add(monster.id);

        // pas encore de catalogue d'objets pour transformer le nom en Equipment
        if (!string.IsNullOrEmpty(monster.item))
            Debug.Log($"{monster.data.monsterClass} laisse tomber : {monster.item}");

        Debug.Log($"{monster.data.monsterClass} est mort");
    }

    private void KillHero(Hero hero)
    {
        hero.isDead = true;
        hero.currentHealth = 0;

        // ses objets restent sur sa case, les autres heros peuvent les ramasser
        foreach (Equipment equipment in hero.inventory)
        {
            lootController.DropLoot(equipment, hero.position);
        }
        hero.inventory.Clear();
        hero.equippedItems.Clear();

        moveHero.UpdatePosition(gameState.heroList);
        Debug.Log($"{hero.data.heroClass} est mort");
    }

    private void UpdateGameStatus()
    {
        if (gameState.gameStatus != GameStatus.Playing) return;

        if (gameState.victory != null)
        {
            foreach (Objective objective in gameState.victory.objectives)
            {
                if (objective.type == ObjectiveType.Tuer && gameState.killedMonsterIds.Contains(objective.target))
                    objective.isDone = true;
                if (objective.type == ObjectiveType.TuerTous && gameState.monsterList.Count == 0)
                    objective.isDone = true;
            }
        }

        bool allHeroesDead = gameState.heroList.Count > 0;
        foreach (Hero hero in gameState.heroList)
        {
            if (!hero.isDead) allHeroesDead = false;
        }

        if (allHeroesDead)
        {
            gameState.gameStatus = GameStatus.Lost;
            Debug.Log("Tous les heros sont morts : partie perdue");
        }
        else if (gameState.victory != null && gameState.victory.IsWon())
        {
            gameState.gameStatus = GameStatus.Won;
            Debug.Log("Quete reussie");
        }
    }

    public bool IsZargonTurn()
    {
        return isZargonTurn;
    }

    public void AddHeroTurn(HeroClass hero) 
    {
        turnOrder.Add(hero);
    }

    public bool GiveSpellType(HeroClass heroClass, SpellType type)
    {
        Hero hero = GetHero(heroClass);

        if (hero == null)
            return false;

        foreach (Hero h in gameState.heroList)
        {
            if (h.spells.Contains(type))
                return false;
        }

        hero.spells.Add(type);
        return true;
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

    public void SpellAction(Spell spell) 
    {
        spellsController.UseSpell(GetCurrentHero(), spell);
        CheckDeaths();
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

    public Hero GetHeroByClass(HeroClass heroClass)
    {
        foreach (Hero hero in gameState.heroList)
        {
            if (hero.data.heroClass == heroClass)
                return hero;
        }

        return null;
    }

    public Hero GetCurrentHero()
    {
        if (turnOrder == null || turnOrder.Count == 0)
            return null;

        if (currentHeroIndex < 0 || currentHeroIndex >= turnOrder.Count)
            return null;

        return GetHeroByClass(turnOrder[currentHeroIndex]);
    }

    [Button]
    public void ChangeQuest(int questNb) 
    {
        
        questNumber = questNb;
        questLoader.LoadQuest(questNb);
        questView.Show(gameState);
        
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
        moveHero.UpdatePosition(gameState.heroList);
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
        CheckDeaths();
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
        CheckDeaths();
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
            if (!hero.isDead && hero.position == new Vector2Int(tile.x, tile.y))
            {
                return hero;
            }
        }
        return null;
    }

    [Button]
    public void ZargonAttackHero() 
    {
        Hero target = GetHeroFromTile(selectedTile);
        if (selectedMonster == null || target == null) return;

        attackController.ZargonConfirmAttack(selectedMonster, target);
        Debug.Log($"Zargon attaque {target.data.heroClass.ToString()} avec {selectedMonster.data.monsterClass.ToString()}");
        CheckDeaths();
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

    public void InitializePlayers(int playerCount)
    {
        players.Clear();
        turnOrder.Clear();
        currentHeroIndex = 0;

        for (int i = 0; i < playerCount; i++)
        {
            players.Add(new Player
            {
                playerID = i + 1
            });
        }
    }

    public bool AssignHero(int playerIndex, Hero hero)
    {
        if (playerIndex < 0 || playerIndex >= players.Count)
            return false;

        if (hero == null)
            return false;

        foreach (Player player in players)
        {
            foreach (Hero assignedHero in player.heroes)
            {
                if (assignedHero.data.heroClass == hero.data.heroClass)
                    return false;
            }
        }

        players[playerIndex].heroes.Add(hero);

        AddHeroTurn(hero.data.heroClass);

        return true;
    }

    public bool AreAllHeroesAssigned()
    {
        int total = 0;

        foreach (Player player in players)
            total += player.heroes.Count;

        return total == 4;
    }
}