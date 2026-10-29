using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public UIManager uiManager;

    [Header("Quête")]
    private int questNumber = 1;

    [Header("État de la partie")]
    private int currentTurn = 1;
    private bool isGameStarted = false;
    private GameState gameState = new GameState();   // doit exister des Awake : les controleurs le gardent

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
    [Header("Plateau")]
    [SerializeField] private BoardBuilder boardBuilder;
    [SerializeField] private QuestView questView;               // optionnel : affiche la quete chargee
    [SerializeField] private bool loadQuestContent = true;      // affiche les donnees de la quete selectionnee
    private QuestLoader questLoader;
    private bool questLoaded;
    private bool questFinished;
    private GUIStyle victoryTitleStyle;
    private GUIStyle victoryTextStyle;
    private GUIStyle victoryButtonStyle;
    private Texture2D victoryButtonTexture;
    private Texture2D victoryButtonHoverTexture;

    private void Awake()
    {
        // le plateau doit exister AVANT les controleurs : MovementSystem garde la Map recue a sa creation
        BuildBoard();

        movementController = new MovementController(this);
        attackController = new AttackController(this);
        potionController = new PotionController(this);
        equipmentController = new EquipmentController(this);
        lootController = new LootController(this);

        //questLoader = new QuestLoader(gameState);
    }
    private void Start()
    {
        // la carte est generee dans Awake (BuildBoard)
        LoadSelectedQuest();


        

        // Show Intro UI
        //

        // Hero Choice
        //uiManager.ShowHeroChoice(true);

        // Spell Choice
        //uiManager.ShowSpellChoice(true);

    }

    // genere le plateau et le donne au GameState
    private void BuildBoard()
    {
        if (boardBuilder == null) boardBuilder = FindFirstObjectByType<BoardBuilder>();
        if (boardBuilder == null)
        {
            Debug.LogError("GameManager : aucun BoardBuilder dans la scene (menu HeroQuest > Installer le menu et le plateau).");
            return;
        }

        boardBuilder.Generate();
        gameState.map = boardBuilder.map;
    }

    // Une quete choisie est toujours chargee depuis sa source (campagne ou joueur).
    // Sans menu, le mode de test peut encore charger questNumber depuis les Resources.
    private void LoadSelectedQuest()
    {
        questLoaded = false;
        questFinished = false;
        if (QuestSelection.HasSelection) questNumber = QuestSelection.Number;
        Debug.Log(QuestSelection.HasSelection
            ? "Quete choisie dans le menu : " + QuestSelection.FileName
            : "Aucune quete choisie dans le menu : quete " + questNumber);

        if (!loadQuestContent || gameState.map == null) return;

        try
        {
            questLoader = new QuestLoader(gameState);
            if (QuestSelection.HasSelection)
            {
                string json = QuestSelection.ReadJson();
                if (string.IsNullOrEmpty(json))
                    throw new System.Exception("Selection invalide ou fichier de quete introuvable : " + QuestSelection.FileName);
                questLoader.Load(json);
            }
            else questLoader.LoadQuest(questNumber);

            questLoaded = true;
            if (questView != null) questView.Show(gameState);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Chargement de la quete : " + e.Message);
        }
    }

    private void Update()
    {
        // Les objectifs doivent etre remplis par le gameplay (Objective.isDone).
        // Simplement lancer/quitter une quete ne valide jamais la campagne.
        if (questLoaded && !questFinished && QuestSelection.HasSelection &&
            gameState.victory != null && gameState.victory.IsWon())
            TryFinishQuest();
    }

    // Point d'entree pour le futur systeme d'objectifs, ou declenchement automatique
    // une fois que Victory.IsWon() devient vrai. Les quetes personnelles ne donnent
    // jamais de progression dans la campagne officielle.
    public bool TryFinishQuest()
    {
        if (questFinished || !questLoaded || !QuestSelection.HasSelection ||
            gameState.victory == null || !gameState.victory.IsWon()) return false;

        bool wasReplay = QuestSelection.IsCampaign && CampaignProgress.IsFinished;
        if (QuestSelection.IsCampaign && !CampaignProgress.TryComplete(QuestSelection.FileName))
        {
            Debug.LogWarning("Campagne : progression non sauvegardee (chapitre inattendu).");
            return false;
        }

        questFinished = true;
        isGameStarted = false;
        Debug.Log(QuestSelection.IsCampaign
            ? (wasReplay ? "Chapitre rejoue : progression de campagne conservee."
                         : "Chapitre de campagne reussi : progression sauvegardee.")
            : "Quete personnelle reussie (progression de campagne inchangee).");
        return true;
    }

    // Fenetre de victoire brun / or, comme les autres menus.
    private static void VictoryFill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private Texture2D VictoryTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        return texture;
    }

    private void InitVictoryStyles()
    {
        if (victoryTitleStyle != null) return;
        Color parchment = new Color(0.95f, 0.88f, 0.70f);
        victoryTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter, fontSize = 30, fontStyle = FontStyle.Bold
        };
        victoryTitleStyle.normal.textColor = new Color(1.00f, 0.82f, 0.30f);
        victoryTextStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter, fontSize = 17, wordWrap = true
        };
        victoryTextStyle.normal.textColor = parchment;
        victoryButtonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter, fontSize = 19, fontStyle = FontStyle.Bold
        };
        victoryButtonTexture = VictoryTexture(new Color(0.25f, 0.14f, 0.08f));
        victoryButtonHoverTexture = VictoryTexture(new Color(0.42f, 0.24f, 0.10f));
        victoryButtonStyle.normal.background = victoryButtonTexture;
        victoryButtonStyle.hover.background = victoryButtonHoverTexture;
        victoryButtonStyle.active.background = victoryButtonHoverTexture;
        victoryButtonStyle.focused.background = victoryButtonTexture;
        victoryButtonStyle.normal.textColor = parchment;
        victoryButtonStyle.hover.textColor = Color.white;
        victoryButtonStyle.active.textColor = Color.white;
        victoryButtonStyle.focused.textColor = parchment;
    }

    private void OnDestroy()
    {
        if (victoryButtonTexture != null) Destroy(victoryButtonTexture);
        if (victoryButtonHoverTexture != null) Destroy(victoryButtonHoverTexture);
    }

    private void OnGUI()
    {
        if (!questFinished) return;
        GUI.depth = -200;
        InitVictoryStyles();

        float width = Mathf.Min(585f, Screen.width - 28f);
        float height = Mathf.Min(350f, Screen.height - 28f);
        Rect dialog = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
        if (Event.current.type == EventType.Repaint)
        {
            VictoryFill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.01f, 0.01f, 0.73f));
            VictoryFill(new Rect(dialog.x + 5f, dialog.y + 6f, width, height), new Color(0f, 0f, 0f, 0.55f));
            VictoryFill(dialog, new Color(0.17f, 0.10f, 0.06f));
        }

        GUI.Label(new Rect(dialog.x + 18f, dialog.y + 18f, dialog.width - 36f, 46f), "Quête réussie !", victoryTitleStyle);
        GUILayout.BeginArea(new Rect(dialog.x + 22f, dialog.y + 74f, dialog.width - 44f, dialog.height - 89f));
        if (QuestSelection.IsCampaign)
        {
            GUILayout.Label(CampaignProgress.IsFinished
                ? "Campagne terminée ! Toutes les quêtes sont désormais rejouables depuis le menu Campagne."
                : "Progression de la campagne sauvegardée.", victoryTextStyle);
            GUILayout.Space(9f);
            CampaignProgress.Chapter next;
            if (CampaignProgress.TryGetNext(out next))
            {
                GUILayout.Label("Prochain chapitre " + next.number + " : " + next.title, victoryTextStyle);
                GUILayout.Space(10f);
                if (GUILayout.Button("Continuer la campagne", victoryButtonStyle, GUILayout.Height(45f)))
                {
                    if (QuestSelection.TrySelectNextCampaign())
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                }
            }
        }
        else GUILayout.Label("Cette quête personnalisée ne modifie pas la campagne.", victoryTextStyle);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Retour au menu principal", victoryButtonStyle, GUILayout.Height(45f)))
        {
            QuestSelection.Clear();
            SceneManager.LoadScene("MainMenu");
        }
        GUILayout.EndArea();
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