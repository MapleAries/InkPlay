namespace InkPlay.Core.Interfaces;

public interface IProjectContext
{
    Guid? CurrentProjectId { get; }
    void SetCurrentProject(Guid projectId);
    void ClearCurrentProject();
}
