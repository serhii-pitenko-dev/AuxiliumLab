using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects;

public record AgentSnapshot
(
    Guid Id,
    ObjectType Type,
    int Speed,
    int SightRange,
    bool IsRun,
    int Stamina,
    int MaxStamina,
    int OrderInTurnQueue);
