using AuxiliumLab.AiSandbox.Ai.Configuration;
using AuxiliumLab.AiSandbox.SharedBaseTypes.ValueObjects.StartupSettings;
using AuxiliumLab.AiSandbox.Startup.Configuration;

namespace AuxiliumLab.AiSandbox.Startup.Menu;

internal class MenuRunner : IMenuRunner
{
    private const string MlpNote = "---Only MLP mode, LSTM has not been implemented yet---";

    public (StartupSettings Settings, ModelType? SelectedAlgorithm) ResolveSettings(StartupSettings defaults)
    {
        var settings = CloneSettings(defaults);

        // ── Step 1: PresentationMode ─────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine(MlpNote);
        Console.WriteLine("Presentation type:");
        Console.WriteLine("1. Console");
        Console.WriteLine("2. Web");
        Console.WriteLine("3. Without visualization (For training and mass run mode)");
        Console.Write("> ");

        settings.PresentationMode = ReadChoice(3) switch
        {
            1 => PresentationMode.Console,
            2 => PresentationMode.Web,
            _ => PresentationMode.WithoutVisualization
        };

        // ── Step 2: ExecutionMode ─────────────────────────────────────────────
        Console.WriteLine();
        Console.WriteLine(MlpNote);
        Console.WriteLine("ExecutionMode:");

        int executionChoice;
        if (settings.PresentationMode == PresentationMode.WithoutVisualization)
        {
            Console.WriteLine("1. Training");
            Console.WriteLine("2. Single Random Ai Simulation");
            Console.WriteLine("3. Single Trained Ai Simulation");
            Console.WriteLine("4. Mass Random AI Simulation");
            Console.WriteLine("5. Mass Trained AI Simulation");
            Console.WriteLine("6. Aggregation Run");
            Console.WriteLine("7. Load Simulation");
            Console.WriteLine("8. Test Preconditions");
            Console.Write("> ");
            executionChoice = ReadChoice(8);

            settings.ExecutionMode = executionChoice switch
            {
                1 => ExecutionMode.Training,
                2 => ExecutionMode.SingleRandomAISimulation,
                3 => ExecutionMode.SingleTrainedAISimulation,
                4 => ExecutionMode.MassRandomAISimulation,
                5 => ExecutionMode.MassTrainedAISimulation,
                6 => ExecutionMode.AggregationRun,
                7 => ExecutionMode.LoadSimulation,
                _ => ExecutionMode.TestPreconditions
            };
        }
        else
        {
            Console.WriteLine("1. Single Random Ai Simulation");
            Console.WriteLine("2. Single Trained Ai Simulation");
            Console.WriteLine("3. Load Simulation");
            Console.WriteLine("4. Test Preconditions");
            Console.Write("> ");
            executionChoice = ReadChoice(4);

            settings.ExecutionMode = executionChoice switch
            {
                1 => ExecutionMode.SingleRandomAISimulation,
                2 => ExecutionMode.SingleTrainedAISimulation,
                3 => ExecutionMode.LoadSimulation,
                _ => ExecutionMode.TestPreconditions
            };
        }

        // ── Step 3: Algorithm (only for Training mode) ────────────────────────
        ModelType? selectedAlgorithm = null;
        if (settings.ExecutionMode == ExecutionMode.Training)
        {
            Console.WriteLine();
            Console.WriteLine("Algorithm:");
            Console.WriteLine("1. PPO");
            Console.WriteLine("2. A2C");
            Console.WriteLine("3. DQN");
            Console.Write("> ");

            selectedAlgorithm = ReadChoice(3) switch
            {
                1 => ModelType.PPO,
                2 => ModelType.A2C,
                _ => ModelType.DQN
            };
        }

        return (settings, selectedAlgorithm);
    }

    private static int ReadChoice(int max)
    {
        while (true)
        {
            var line = Console.ReadLine()?.Trim();
            if (int.TryParse(line, out int choice) && choice >= 1 && choice <= max)
                return choice;

            Console.Write($"Please enter a number between 1 and {max}: ");
        }
    }

    private static StartupSettings CloneSettings(StartupSettings s) => new()
    {
        IsPreconditionStart = s.IsPreconditionStart,
        PresentationMode = s.PresentationMode,
        ExecutionMode = s.ExecutionMode,
        TestPreconditionsEnabled = s.TestPreconditionsEnabled,
        IsWebApiEnabled = s.IsWebApiEnabled,
        PolicyType = s.PolicyType,
        StandardSimulationCount = s.StandardSimulationCount,
        IncrementalProperties = s.IncrementalProperties
    };
}
