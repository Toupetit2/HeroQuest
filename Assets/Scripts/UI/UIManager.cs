using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;

    [Header("Menus")]
    public GameObject intro;
    public GameObject spellChoice;
    public GameObject playerCountSelection;
    public GameObject heroSelection;
    public GameObject gameUI;
    public GameObject actionsButtons;
    public GameObject confirmMoveButton;
    public GameObject confirmAttackButton;

    [Header("UI Text")]
    public Text turnText;
    public Text currentPlayerText;

    [Header("Hero Selection Buttons")]
    public GameObject barbarianButton;
    public GameObject dwarfButton;
    public GameObject elfButton;
    public GameObject wizardButton;

    [Header("Spell Selection")]
    public Text currentSpellPickerText;

    public GameObject waterButton;
    public GameObject airButton;
    public GameObject fireButton;
    public GameObject earthButton;

    private int currentSpellChoiceIndex = 0;
    private int currentPlayerIndex = 0;

    private HeroClass[] spellChoiceOrder =
    {
        HeroClass.Wizard,
        HeroClass.Elf,
        HeroClass.Wizard,
        HeroClass.Wizard
    };

    void Awake()
    {
        intro.SetActive(true);
        spellChoice.SetActive(false);
        playerCountSelection.SetActive(false);
        heroSelection.SetActive(false);
        gameUI.SetActive(false);
        turnText.gameObject.SetActive(false);

        // bouton "i" sur chaque heros : ouvre sa fiche (caracteristiques et description)
        heroSelection.AddComponent<HeroInfoUI>().Setup(gameManager,
            new[] { barbarianButton, dwarfButton, elfButton, wizardButton });

        // explication sous le choix du nombre de joueurs : les 4 heros jouent toujours
        Text title = playerCountSelection.GetComponentInChildren<Text>(true);
        Text explanation = UIFactory.Label("AllHeroesPlay", playerCountSelection.transform,
            "All 4 heroes always take part in the quest.\nWith fewer than 4 players, some players control several heroes.",
            30, UIFactory.Ivory, TextAnchor.MiddleCenter, new Vector2(1400f, 100f), new Vector2(0f, -220f), title != null ? title.font : null);
        explanation.horizontalOverflow = HorizontalWrapMode.Overflow;

        // menu pause : bouton "Menu" en haut a droite ou touche Echap
        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();
    }

    ///////////////////////////
    ////       INTRO       ////
    ///////////////////////////

    public void PassIntro()
    {
        intro.SetActive(false);
        playerCountSelection.SetActive(true);
    }

    ///////////////////////////
    ////   PLAYER COUNT   ////
    ///////////////////////////

    public void SelectPlayerCount(int count)
    {
        gameManager.InitializePlayers(count);

        playerCountSelection.SetActive(false);
        heroSelection.SetActive(true);

        currentPlayerIndex = 0;

        barbarianButton.SetActive(true);
        dwarfButton.SetActive(true);
        elfButton.SetActive(true);
        wizardButton.SetActive(true);

        UpdateHeroSelectionUI();
    }

    ///////////////////////////
    ////   HERO SELECTION  ////
    ///////////////////////////

    public void UpdateHeroSelectionUI()
    {
        // les 4 heros jouent toujours : avec moins de 4 joueurs, certains joueurs en controlent plusieurs
        // les heros sont distribues a tour de role (joueur 1, 2... puis on recommence)
        int playerCount = Mathf.Max(1, gameManager.players.Count);
        int heroesForPlayer = 4 / playerCount + (currentPlayerIndex < 4 % playerCount ? 1 : 0);
        int alreadyChosen = gameManager.players[currentPlayerIndex].heroes.Count;

        currentPlayerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        currentPlayerText.verticalOverflow = VerticalWrapMode.Overflow;   // deux lignes
        currentPlayerText.supportRichText = true;
        currentPlayerText.text = "Player " + (currentPlayerIndex + 1) + ", choose your hero " + (alreadyChosen + 1) + " of " + heroesForPlayer
            + "\n<size=28>All 4 heroes always play. Click \"i\" to see a hero's card.</size>";
    }

    public void SelectHero(HeroClass heroClass)
    {
        Hero hero = gameManager.GetHeroByClass(heroClass);

        if (!gameManager.AssignHero(currentPlayerIndex, hero))
            return;

        HideSelectedHeroButton(heroClass);

        if (gameManager.AreAllHeroesAssigned())
        {
            ConfirmHeroSelection();
            return;
        }

        currentPlayerIndex++;

        if (currentPlayerIndex >= gameManager.players.Count)
            currentPlayerIndex = 0;

        UpdateHeroSelectionUI();
    }

    public void SelectBarbarian()
    {
        SelectHero(HeroClass.Barbarian);
    }

    public void SelectDwarf()
    {
        SelectHero(HeroClass.Dwarf);
    }

    public void SelectElf()
    {
        SelectHero(HeroClass.Elf);
    }

    public void SelectWizard()
    {
        SelectHero(HeroClass.Wizard);
    }

    public void ConfirmHeroSelection()
    {
        if (!gameManager.AreAllHeroesAssigned())
        {
            Debug.LogWarning("All four heroes must be assigned!");
            return;
        }

        heroSelection.SetActive(false);
        spellChoice.SetActive(true);

        currentSpellChoiceIndex = 0;

        ResetSpellButtons();
        UpdateSpellSelectionUI();
    }

    private void HideSelectedHeroButton(HeroClass heroClass)
    {
        switch (heroClass)
        {
            case HeroClass.Barbarian:
                barbarianButton.SetActive(false);
                break;

            case HeroClass.Dwarf:
                dwarfButton.SetActive(false);
                break;

            case HeroClass.Elf:
                elfButton.SetActive(false);
                break;

            case HeroClass.Wizard:
                wizardButton.SetActive(false);
                break;
        }
    }

    ////////////////////////////
    ////    Spell Choice    ////
    ////////////////////////////

    private void UpdateSpellSelectionUI()
    {
        HeroClass currentHero = spellChoiceOrder[currentSpellChoiceIndex];

        // le heros choisit UNE ecole a la fois, puis la main passe au suivant de spellChoiceOrder :
        // on dit clairement qui choisit, combien il lui en reste, et qui choisira ensuite
        int total = 0, done = 0;
        for (int i = 0; i < spellChoiceOrder.Length; i++)
        {
            if (spellChoiceOrder[i] != currentHero) continue;
            total++;
            if (i < currentSpellChoiceIndex) done++;
        }
        int remaining = total - done;

        string name = "<color=" + HeroColors.Hex(currentHero) + ">" + currentHero + "</color>";
        string line;
        if (total == 1) line = name + ": choose your school of spells";
        else if (done == 0) line = name + ": choose 1 school of spells (" + total + " in all)";
        else if (remaining == 1) line = name + ": choose your last school of spells (" + done + "/" + total + " chosen)";
        else line = name + ": choose " + remaining + " more schools of spells, one at a time (" + done + "/" + total + " chosen)";

        // qui choisit ensuite ; au premier choix, on annonce tout l'ordre pour ne surprendre personne
        string next;
        if (currentSpellChoiceIndex == 0) next = "Schools are picked in turn: " + OrderText() + ".";
        else if (currentSpellChoiceIndex + 1 < spellChoiceOrder.Length) next = "Next: " + spellChoiceOrder[currentSpellChoiceIndex + 1] + ".";
        else next = "Last choice, then the quest begins.";

        currentSpellPickerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        currentSpellPickerText.verticalOverflow = VerticalWrapMode.Overflow;   // deux lignes
        currentSpellPickerText.supportRichText = true;
        currentSpellPickerText.text = line + "\n<size=28>" + next + "</size>";
    }

    // "Wizard, Elf, then Wizard twice" a partir de spellChoiceOrder
    private string OrderText()
    {
        System.Collections.Generic.List<string> parts = new System.Collections.Generic.List<string>();
        int i = 0;
        while (i < spellChoiceOrder.Length)
        {
            int count = 1;
            while (i + count < spellChoiceOrder.Length && spellChoiceOrder[i + count] == spellChoiceOrder[i]) count++;
            parts.Add(spellChoiceOrder[i] + (count == 2 ? " twice" : count > 2 ? " " + count + " times" : ""));
            i += count;
        }
        if (parts.Count == 1) return parts[0];
        return string.Join(", ", parts.GetRange(0, parts.Count - 1).ToArray()) + ", then " + parts[parts.Count - 1];
    }

    public void SelectWater()
    {
        SelectSpellType(SpellType.Water);
    }

    public void SelectAir()
    {
        SelectSpellType(SpellType.Air);
    }

    public void SelectFire()
    {
        SelectSpellType(SpellType.Fire);
    }

    public void SelectEarth()
    {
        SelectSpellType(SpellType.Earth);
    }

    private void SelectSpellType(SpellType spellType)
    {
        HeroClass currentHero = spellChoiceOrder[currentSpellChoiceIndex];

        if (!gameManager.GiveSpellType(currentHero, spellType))
            return;

        HideSelectedSpellButton(spellType);

        currentSpellChoiceIndex++;

        // Les quatre types ont été attribués
        if (currentSpellChoiceIndex >= spellChoiceOrder.Length)
        {
            spellChoice.SetActive(false);
            gameManager.StartGame();
            turnText.gameObject.SetActive(true);
            return;
        }

        UpdateSpellSelectionUI();
    }

    private void HideSelectedSpellButton(SpellType spellType)
    {
        switch (spellType)
        {
            case SpellType.Water:
                waterButton.SetActive(false);
                break;

            case SpellType.Air:
                airButton.SetActive(false);
                break;

            case SpellType.Fire:
                fireButton.SetActive(false);
                break;

            case SpellType.Earth:
                earthButton.SetActive(false);
                break;
        }
    }

    private void ResetSpellButtons()
    {
        waterButton.SetActive(true);
        airButton.SetActive(true);
        fireButton.SetActive(true);
        earthButton.SetActive(true);
    }

    ////////////////////////////
    ////      Game UI       ////
    ////////////////////////////

    public void ShowGameUI()
    {
        gameUI.SetActive(true);
        confirmAttackButton.SetActive(false);
        confirmMoveButton.SetActive(false);
    }

    public void ShowActionsButtons(bool state)
    {
        actionsButtons.SetActive(state);
    }

    public void MoveActions()
    {
        ShowActionsButtons(false);
        confirmMoveButton.SetActive(true);
        gameManager.MovementRollDices();
    }

    public void ConfirmMoveActions()
    {
        ShowActionsButtons(true);
        confirmMoveButton.SetActive(false);
        gameManager.MovementConfirmMove();
    }

    public void AttackActions()
    {
        ShowActionsButtons(false);
        confirmAttackButton.SetActive(true);
        gameManager.AttackStart();
    }

    public void ConfirmAttackActions()
    {
        ShowActionsButtons(true);
        confirmAttackButton.SetActive(false);
        gameManager.AttackConfirm();
    }

    public void SetTurnText()
    {
        var currentHero = gameManager.GetCurrentHero();

        // pastille de la couleur du heros (la meme que son pion sur le plateau)
        if (currentHero != null)
        {
            HeroClass heroClass = currentHero.data.heroClass;
            turnText.supportRichText = true;
            turnText.horizontalOverflow = HorizontalWrapMode.Overflow;   // sinon le nom du heros passe a la ligne et disparait
            turnText.alignment = TextAnchor.MiddleLeft;                  // le texte part du bord gauche du cadre : la pastille reste a l'ecran
            turnText.text = "<color=" + HeroColors.Hex(heroClass) + ">●</color> Turn of " + heroClass;
        }
    }
}