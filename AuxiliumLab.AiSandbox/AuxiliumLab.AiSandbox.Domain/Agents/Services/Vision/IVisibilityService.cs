using AuxiliumLab.AiSandbox.Domain.Agents.Entities;
using AuxiliumLab.AiSandbox.Domain.Maps;
using AuxiliumLab.AiSandbox.Domain.Playgrounds;

namespace AuxiliumLab.AiSandbox.Domain.Agents.Services.Vision;

public interface IVisibilityService
{
    void UpdateVisibleCells(Agent agent, StandardPlayground playground);
}