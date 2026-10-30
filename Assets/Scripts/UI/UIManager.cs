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
        currentPlayerText.text = "Player " + (currentPlayerIndex + 1);
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

        currentSpellPickerText.text = currentHero + " chooses a spell";
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

        if (currentHero != null)
            turnText.text = "Turn of " + currentHero.data.heroClass.ToString();
    }
}