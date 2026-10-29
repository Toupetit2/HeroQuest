using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Quête")]
    public int questNumber = 1;

    [Header("État de la partie")]
    public int currentTurn = 1;
    public bool isGameStarted = false;
    public GameState gameState;

    public List<HeroClass> turnOrder = new List<HeroClass>();

    public int currentPlayerIndex = 0;

    [Header("État du tour")]    
    public bool hasMoveAction = true;
    public bool hasAction = true;

    public void Test() 
    {
        return;
    }

    // Le GameManager pilote désormais la création du plateau et le choix du mode.
    // Pas de code UnityEditor : fonctionne en Play et dans une build.
    private HQGameModeRuntime questRuntime;

    private void Awake()
    {
        if (gameState == null) gameState = new GameState();
        if (gameState.heroList == null) gameState.heroList = new List<Hero>();
        if (gameState.monsterList == null) gameState.monsterList = new List<Monster>();
        if (turnOrder == null) turnOrder = new List<HeroClass>();

        questRuntime = GetComponent<HQGameModeRuntime>();
        if (questRuntime == null) questRuntime = gameObject.AddComponent<HQGameModeRuntime>();
        questRuntime.manager = this;
    }

    private void Start()
    {
        // On ne crée plus une carte 4x3 temporaire ici.
        // La carte 26x19 est fournie par HQGameModeRuntime lors du choix de quête.
    }

    // Méthodes appelables par une interface de jeu, pas par l'éditeur Unity.
    public bool LancerCampagne(int numeroQuete)
    {
        if (questRuntime == null) return false;
        bool result = questRuntime.ChargerCampagne(numeroQuete);
        if (result) DebuterNouvelleQuete(numeroQuete);
        return result;
    }

    public bool LancerProcedural(HQGameModeRuntime.Options generation)
    {
        if (questRuntime == null) return false;
        bool result = questRuntime.GenererProcedural(generation);
        if (result) DebuterNouvelleQuete(0);
        return result;
    }

    public void RevenirAuChoixDuMode()
    {
        if (questRuntime != null) questRuntime.EffacerQuete();
        isGameStarted = false;
        if (gameState != null) gameState.map = null;
    }

    private void DebuterNouvelleQuete(int numeroQuete)
    {
        questNumber = numeroQuete;
        currentTurn = 1;
        currentPlayerIndex = 0;
        isGameStarted = false; // Attendre la sélection des héros avant StartGame().
        hasMoveAction = true;
        hasAction = true;
        if (gameState == null) gameState = new GameState();
        gameState.map = new Map(questRuntime.PlateauLogique.text);
        // La PCG configure le décor. Les systèmes de héros/combat de l'équipe
        // doivent ensuite utiliser cette carte et connecter les interactions.
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

}

