using InkPlay.Core.Interfaces;

namespace InkPlay.Services;

public class ProjectContext : IProjectContext
{
    public Guid? CurrentProjectId { get; private set; }

    public void SetCurrentProject(Guid projectId)
    {
        CurrentProjectId = projectId;
    }

    public void ClearCurrentProject()
    {
        CurrentProjectId = null;
    }
}
