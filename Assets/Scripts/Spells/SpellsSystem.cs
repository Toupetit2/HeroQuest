using System.Collections.Generic;
using UnityEngine;

public class SpellsSystem
{

    private Dictionary<SpellType, List<Spell>> spellDict = new Dictionary<SpellType, List<Spell>>();

    public void Init(Spell[] allSpells)
    {
        spellDict.Clear();

        foreach (Spell spell in allSpells)
            AddSpell(spell.spellType, spell);
    }

    private void AddSpell(SpellType type, Spell spell)
    {
        if (!spellDict.TryGetValue(type, out List<Spell> list))
        {
            list = new List<Spell>();
            spellDict.Add(type, list);
        }
        list.Add(spell);
    }

    public List<Spell> GetSpells(SpellType spellType)
    {
        if (spellDict.TryGetValue(spellType, out List<Spell> list))
            return list;

        return new List<Spell>();
    }
}
