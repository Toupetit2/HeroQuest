using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject intro;
    public GameObject heroChoice;
    public GameObject spellChoice;
    public GameObject gameUI;

    void Awake()
    {
        intro.SetActive(true);
        heroChoice.SetActive(false);
        spellChoice.SetActive(false);
        gameUI.SetActive(false);
    }

      ///////////////////////////
     ////       Intro       ////
    ///////////////////////////
    
    public void PassIntro()
    {
        intro.SetActive(false);
        heroChoice.SetActive(true);
    }

      ///////////////////////////
     ////    Hero Choice    ////
    ///////////////////////////

    public void ChooseBarbarian()
    {
        heroChoice.SetActive(false);
        // Set Hero to Barbarian
        spellChoice.SetActive(true);
    }

    public void ChooseDwarf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Dwar
        spellChoice.SetActive(true);
    }

    public void ChooseElf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Elf
        spellChoice.SetActive(true);
    }

    public void ChooseWizard()
    {
        heroChoice.SetActive(false);
        // Set Hero to Wizar
        spellChoice.SetActive(true);
    }

      ////////////////////////////
     ////    Spell Choice    ////
    ////////////////////////////

    public void ChooseWater()
    {
        spellChoice.SetActive(false);
        // Set Hero to Water
        gameManager.StartGame();
    }

    public void ChooseAir()
    {
        spellChoice.SetActive(false);
        // Set Hero to Air
        gameManager.StartGame();
    }

    public void ChooseFire()
    {
        spellChoice.SetActive(false);
        // Set Hero to Fire
        gameManager.StartGame();
    }

    public void ChooseEarth()
    {
        spellChoice.SetActive(false);
        // Set Hero to Earth
        gameManager.StartGame();
    }

      ////////////////////////////
     ////      Game UI       ////
    ////////////////////////////
    
    public void ShowGameUI()
    {
        gameUI.SetActive(true);
    }
}