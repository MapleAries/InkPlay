using InkPlay.Core.Models;
using LiteDB;

namespace InkPlay.Services.Data;

public class InkPlayDbContext : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public InkPlayDbContext()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InkPlay",
            "inkplay.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var connectionString = new ConnectionString(dbPath)
        {
            Connection = ConnectionType.Shared
        };
        _db = new LiteDatabase(connectionString);
    }

    public ILiteCollection<Project> Projects => _db.GetCollection<Project>("projects");
    public ILiteCollection<Document> Documents => _db.GetCollection<Document>("documents");
    public ILiteCollection<Character> Characters => _db.GetCollection<Character>("characters");
    public ILiteCollection<CharacterRelationship> Relationships
        => _db.GetCollection<CharacterRelationship>("relationships");
    public ILiteCollection<WorldSetting> WorldSettings => _db.GetCollection<WorldSetting>("world_settings");
    public ILiteCollection<AiConversation> Conversations => _db.GetCollection<AiConversation>("conversations");
    public ILiteCollection<Voice> Voices => _db.GetCollection<Voice>("voices");
    public ILiteCollection<GlossaryEntry> GlossaryEntries => _db.GetCollection<GlossaryEntry>("glossary_entries");
    public ILiteCollection<DocumentVersion> DocumentVersions => _db.GetCollection<DocumentVersion>("document_versions");

    /// <summary>
    /// Executes a write operation under a mutex lock to prevent concurrent database writes.
    /// </summary>
    public async Task<T> ExecuteWriteAsync<T>(Func<T> operation)
    {
        await _writeLock.WaitAsync();
        try
        {
            return operation();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Executes a write operation under a mutex lock to prevent concurrent database writes.
    /// </summary>
    public async Task ExecuteWriteAsync(Action operation)
    {
        await _writeLock.WaitAsync();
        try
        {
            operation();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        _writeLock.Dispose();
        _db.Dispose();
    }
}
