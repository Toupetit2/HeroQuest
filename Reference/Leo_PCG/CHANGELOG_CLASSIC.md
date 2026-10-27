# Changes compared with HeroQuest_PCG_Options_Projet.zip

- `Script/HeroQuestSimplePCG.cs`: stock limits, secret doors as optional shortcuts, 2×2 stairs, hidden room contents, hidden traps, quest chest, blocked tiles, entry/goal rooms, more deterministic metadata, door count cap.
- `Script/HeroQuestPCGMenu.cs`: added classic-play switches, obstacle count and debug reveals.
- `Script/HQGeneratedDoor.cs`: one-way door state (closed → open), secret search/reveal, room reveal hooks.
- NEW `Script/HQClassicSupplies.cs`: box stock counts and placeholder sample definitions.
- NEW `Script/HQHiddenTrap.cs`: hidden/discovered/triggered trap state with hooks for later gameplay.
- NEW `Script/HQPlacedPiece.cs`: track IDs, footprint, room and quest objective on generated GameObjects.
- NEW `Tests/test_classic_logic.py`: offline graph tests on the actual provided `Plateau.prefab` layout.

The floor, walls, room map, existing prefabs and scene assets are retained. This is a procedural *quest-layout* update, not a full rule implementation. See README_CLASSIC_EN.md for exact scope and setup.
