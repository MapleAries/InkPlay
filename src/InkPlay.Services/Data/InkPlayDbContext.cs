using InkPlay.Core.Models;
using LiteDB;

namespace InkPlay.Services.Data;

public class InkPlayDbContext : IDisposable
{
    private readonly LiteDatabase _db;

    public InkPlayDbContext()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InkPlay",
            "inkplay.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _db = new LiteDatabase(dbPath);
    }

    public ILiteCollection<Project> Projects => _db.GetCollection<Project>("projects");
    public ILiteCollection<Document> Documents => _db.GetCollection<Document>("documents");
    public ILiteCollection<Character> Characters => _db.GetCollection<Character>("characters");
    public ILiteCollection<CharacterRelationship> Relationships
        => _db.GetCollection<CharacterRelationship>("relationships");
    public ILiteCollection<WorldSetting> WorldSettings => _db.GetCollection<WorldSetting>("world_settings");
    public ILiteCollection<AiConversation> Conversations => _db.GetCollection<AiConversation>("conversations");

    public void Dispose() => _db.Dispose();
}
