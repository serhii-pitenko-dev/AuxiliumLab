using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuxiliumLab.AiSandbox.Domain.Agents.Entities;

namespace AuxiliumLab.AiSandbox.Domain.Validation.Agents;

internal class AgentActionAddValidator
{
    public void Validate(Agent agent, List<AgentAction> existActions, List<AgentAction> newActions)
    {
        var moveCount = existActions.Where(a => a == AgentAction.Move).Count() + newActions.Where(a => a == AgentAction.Move).Count();

        if (agent.Stamina < moveCount)
        {
            throw new InvalidOperationException("Agent does not have enough stamina for the requested move actions.");
        }
    }
}

