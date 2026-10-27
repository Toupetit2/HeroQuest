using System.Collections.Generic;
using UnityEngine;

// Physical miniature/furniture quantities listed on page 4 of the HeroQuest rulebook.
// These are limits for a classic-style *single dungeon*, not encounter probabilities.
public static class HQClassicSupplies
{
    public static int Maximum(HeroQuestSimplePCG.ContentKind kind, int id)
    {
        if (kind == HeroQuestSimplePCG.ContentKind.Monster)
        {
            switch (id)
            {
                case 5: return 2;  // Zombies
                case 6: return 2;  // Mummies
                case 7: return 1;  // Gargoyle
                case 8: return 4;  // Skeletons
                case 9: return 6;  // Goblins
                case 10: return 8; // Orcs
                case 11: return 3; // Abominations
                case 12: return 4; // Dread warriors
                case 27: return 1; // Dread sorcerer
            }
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Furniture)
        {
            switch (id)
            {
                case 13: return 3; // Chests
                case 14: return 1; // Tomb
                case 16: return 2; // Bookcases
                case 17: return 2; // Tables
                case 19: return 1; // Sorcerer's table
                case 20: return 1; // Alchemist's bench
                case 21: return 1; // Throne
                case 22: return 1; // Fireplace
                case 23: return 1; // Cupboard
                case 24: return 1; // Rack
                case 25: return 1; // Weapons rack
            }
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Obstacle)
        {
            // Counts of blocked cardboard tiles aren't listed in the page 4 stock list.
            if (id == 15 || id == 18) return int.MaxValue;
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Trap)
        {
            // Dungeon.pdf identifies trap TYPES, but doesn't specify stock quantities.
            // Do not invent a stock count; the generator caps their overall count.
            if (id == 7 || id == 8 || id == 9) return int.MaxValue;
        }
        return 0; // Not a standard randomly placeable piece in this category.
    }

    // Temporary samples when the real 3D prefabs haven't been linked yet.
    // Footprints are illustrative defaults; configure the actual models in the Inspector.
    public static List<HeroQuestSimplePCG.Prop> Demo(HeroQuestSimplePCG.ContentKind kind,
                                                      bool includeQuestBosses)
    {
        var list = new List<HeroQuestSimplePCG.Prop>();
        if (kind == HeroQuestSimplePCG.ContentKind.Monster)
        {
            Add(list, kind, 9, "Goblin", 9, 1, 1);
            Add(list, kind, 10, "Orc", 8, 1, 1);
            Add(list, kind, 8, "Skeleton", 6, 1, 1);
            Add(list, kind, 5, "Zombie", 3, 1, 1);
            Add(list, kind, 6, "Mummy", 3, 1, 1);
            Add(list, kind, 11, "Abomination", 2, 1, 1);
            Add(list, kind, 12, "Dread warrior", 2, 1, 1);
            if (includeQuestBosses)
            {
                Add(list, kind, 7, "Gargoyle", 1, 1, 1);
                Add(list, kind, 27, "Dread sorcerer", 1, 1, 1);
            }
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Furniture)
        {
            Add(list, kind, 13, "Chest", 5, 1, 1);
            Add(list, kind, 16, "Bookcase", 3, 2, 1, true);
            Add(list, kind, 17, "Table", 3, 2, 2);
            Add(list, kind, 23, "Cupboard", 2, 2, 1, true);
            Add(list, kind, 25, "Weapons rack", 2, 2, 1, true);
            Add(list, kind, 24, "Rack", 2, 2, 1, true);
            Add(list, kind, 22, "Fireplace", 1, 2, 1, true);
            Add(list, kind, 21, "Throne", 1, 1, 1);
            Add(list, kind, 14, "Tomb", 1, 2, 1);
            Add(list, kind, 19, "Sorcerer's table", 1, 2, 1);
            Add(list, kind, 20, "Alchemist's bench", 1, 2, 1);
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Obstacle)
        {
            Add(list, kind, 15, "Blocked square", 5, 1, 1);
            Add(list, kind, 18, "Double blocked square", 2, 2, 1);
        }
        else if (kind == HeroQuestSimplePCG.ContentKind.Trap)
        {
            Add(list, kind, 7, "Falling block", 2, 1, 1);
            Add(list, kind, 8, "Pit", 4, 1, 1);
            Add(list, kind, 9, "Spear", 4, 1, 1);
        }
        return list;
    }

    private static void Add(List<HeroQuestSimplePCG.Prop> list,
                            HeroQuestSimplePCG.ContentKind kind, int id,
                            string name, int weight, int width, int height,
                            bool againstWall = false)
    {
        list.Add(new HeroQuestSimplePCG.Prop
        {
            kind = kind, id = id, description = name, weight = weight,
            sizeInCells = new Vector2Int(width, height), canRotate = true,
            againstWall = againstWall
        });
    }
}
