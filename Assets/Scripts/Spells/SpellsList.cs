using System.Collections.Generic;
using System;

Dictionary<SpellType, List<Spell>> spellDict = new Dictionary<SpellType, List<Spell>>();
List<Spell> fire = new List<Spell>();

spellDict.Add(SpellType.Fire, BouleDeFeu);
