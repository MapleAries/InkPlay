namespace InkPlay.Core.Models;

public class ScriptScene
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SceneHeading { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TimeOfDay { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public List<SceneDialogue> Dialogues { get; set; } = new();
}

public class SceneDialogue
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
    public string Parenthetical { get; set; } = string.Empty;
}
