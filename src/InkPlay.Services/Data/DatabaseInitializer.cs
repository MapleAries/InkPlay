namespace InkPlay.Services.Data;

public static class DatabaseInitializer
{
    public static void Initialize(InkPlayDbContext db)
    {
        db.Projects.EnsureIndex(x => x.UpdatedAt);
        db.Documents.EnsureIndex(x => x.ProjectId);
        db.Documents.EnsureIndex(x => x.SortOrder);
        db.Characters.EnsureIndex(x => x.ProjectId);
        db.Relationships.EnsureIndex(x => x.ProjectId);
        db.WorldSettings.EnsureIndex(x => x.ProjectId);
        db.Conversations.EnsureIndex(x => x.ProjectId);
    }
}
