public class Objective
{
    public ObjectiveType type;
    public string target;   // id du monstre, de l'objet ou du personnage selon le type
    public int count;       // pour Rapporter
    public string room;     // pour TrouverObjet (ex : "S11")
    public bool isDone;     // rempli par le gameplay

    public Objective(ObjectiveType type)
    {
        this.type = type;
    }
}
