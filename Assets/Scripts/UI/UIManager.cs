using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject heroChoice;
    public GameObject spellChoice;

    void Awake()
    {
        heroChoice.SetActive(false);
        spellChoice.SetActive(false);
    }

      ///////////////////////////
     ////   Hero Choice     ////
    ///////////////////////////
    public void ShowHeroChoice(bool state)
    {
        heroChoice.SetActive(state);
    }

    public void ChooseBarbarian()
    {
        heroChoice.SetActive(false);
        // Set Hero to Barbarian
    }

    public void ChooseDwarf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Dwarf
    }

    public void ChooseElf()
    {
        heroChoice.SetActive(false);
        // Set Hero to Elf
    }

    public void ChooseWizard()
    {
        heroChoice.SetActive(false);
        // Set Hero to Wizard
    }

      ////////////////////////////
     ////   Spell Choice     ////
    ////////////////////////////
    public void ShowSpellChoice(bool state)
    {
        spellChoice.SetActive(state);
    }

    public void ChooseWater()
    {
        spellChoice.SetActive(false);
        // Set Hero to Water
    }

    public void ChooseAir()
    {
        spellChoice.SetActive(false);
        // Set Hero to Air
    }

    public void ChooseFire()
    {
        spellChoice.SetActive(false);
        // Set Hero to Fire
    }

    public void ChooseEarth()
    {
        spellChoice.SetActive(false);
        // Set Hero to Earth
    }

}