namespace AuxiliumLab.AiSandbox.Ai.Configuration;

/// <summary>
/// Defines the type of neural network policy used by the RL agent.
/// </summary>
public enum AiPolicy
{
    /// <summary>
    /// Feed-forward Multi-Layer Perceptron policy.
    /// Does not maintain internal state between steps.
    /// All required information must be provided in the current observation.
    /// </summary>
    MLP = 0,

    /// <summary>
    /// Recurrent policy based on LSTM (Long Short-Term Memory).
    /// Maintains hidden state between steps and can model temporal dependencies.
    /// Suitable for partially observable environments.
    /// </summary>
    LSTM
}

