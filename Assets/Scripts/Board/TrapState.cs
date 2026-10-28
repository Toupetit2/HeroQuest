public enum TrapState
{
    Cache,      // personne ne le connait
    Decouvert,  // trouve par une recherche, rien n'est pose sur le plateau
    Declenche,  // un heros est tombe dedans, la tuile est posee
    Desarme     // retire, la case redevient normale
}
