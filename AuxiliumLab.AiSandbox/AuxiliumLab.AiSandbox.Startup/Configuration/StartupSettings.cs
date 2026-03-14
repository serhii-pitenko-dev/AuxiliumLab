using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuxiliumLab.AiSandbox.Startup.Configuration;

public class StartupSettings
{
    public bool IsPreconditionStart { get; set; }
    public PresentationMode PresentationMode { get; set; }
    public ExecutionMode ExecutionMode { get; set; }

    /// <summary>
    /// The RL algorithm to use for training or for selecting the trained model at inference time.
    /// When ExecutionMode is SingleTrainedAISimulation or MassTrainedAISimulation the latest model
    /// file inside FileStorage.BasePath / FileStorage.TrainedAlgorithms / Algorithm is loaded
    /// automatically.
    /// </summary>
    public ModelType Algorithm { get; set; } = ModelType.PPO;
    public bool TestPreconditionsEnabled { get; set; }
    public bool IsWebApiEnabled { get; set; }
    public AiPolicy PolicyType { get; set; }
    public int StandardSimulationCount { get; set; } = 1;
    public IncrementalPropertiesSettings IncrementalProperties { get; set; } = new();
}

