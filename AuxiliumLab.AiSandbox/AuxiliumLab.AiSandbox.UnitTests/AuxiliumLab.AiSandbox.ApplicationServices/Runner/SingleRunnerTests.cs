using AuxiliumLab.AiSandbox.ApplicationServices.Executors;
using AuxiliumLab.AiSandbox.ApplicationServices.Runner.SingleRunner;
using AuxiliumLab.AiSandbox.Infrastructure.Configuration.Preconditions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.ApplicationServices.Runner;

/// <summary>
/// Unit tests for <see cref="SingleRunner"/>.
///
/// <para>
/// <b>Context — console vs. non-console routing in Program.cs</b>
/// </para>
/// <para>
/// <c>Program.cs</c> contains the following routing logic for
/// <c>ExecutionMode.SingleTrainedAISimulation</c>:
/// <list type="bullet">
///   <item>
///     <b>Console presentation</b> — resolves <see cref="IExecutorForPresentation"/> from the
///     DI scope and dispatches through <see cref="SingleRunner.RunSingleAsync"/>.
///     This is required so that game events are published to the shared
///     <c>IMessageBroker</c> singleton, which <c>ConsoleRunner</c> subscribes to for
///     live rendering.  <c>IAiActions</c> is already overridden to
///     <c>InferenceActions</c> in the container, so the trained model is still used.
///   </item>
///   <item>
///     <b>All other presentation modes</b> — creates an <see cref="IStandardExecutor"/> via
///     <c>IExecutorFactory.CreateStandardExecutor()</c> and dispatches through
///     <see cref="SingleRunner.RunSingleTrainedAsync"/>.
///     The standard executor intentionally uses a private <c>MessageBroker</c> instance
///     (for parallelism isolation) so it must NOT be used when console rendering is active.
///   </item>
/// </list>
/// </para>
/// <para>
/// Since <c>Program.cs</c> uses top-level statements it cannot be instantiated in a
/// unit test.  These tests therefore verify the <see cref="SingleRunner"/> dispatch
/// contract: each method must call <c>RunAsync()</c> on the executor it receives,
/// which is the guarantee that the correct executor wires up to the correct
/// presentation layer.
/// </para>
/// </summary>
[TestClass]
public class SingleRunnerTests
{
    private SandBoxConfiguration _config = null!;

    [TestInitialize]
    public void Setup()
    {
        _config = new SandBoxConfiguration
        {
            MaxTurns = new IncrementalRange { Current = 0 }
        };
    }

    // ── RunSingleAsync ────────────────────────────────────────────────────────

    /// <summary>
    /// Console path for both <c>SingleRandomAISimulation</c> and
    /// <c>SingleTrainedAISimulation</c>: Program.cs resolves
    /// <see cref="IExecutorForPresentation"/> and calls <see cref="SingleRunner.RunSingleAsync"/>.
    /// The runner must delegate directly to <see cref="IExecutor.RunAsync()"/>.
    /// </summary>
    [TestMethod]
    public async Task RunSingleAsync_CallsRunAsyncOnPresentationExecutor()
    {
        var mockExecutor = new Mock<IExecutorForPresentation>();
        mockExecutor.Setup(e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()))
                    .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunSingleAsync(mockExecutor.Object);

        mockExecutor.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Once,
            "RunSingleAsync must call RunAsync() exactly once on the presentation executor");
    }

    /// <summary>
    /// Verifies the runner does not invoke any method other than <c>RunAsync</c>
    /// on the presentation executor.
    /// </summary>
    [TestMethod]
    public async Task RunSingleAsync_DoesNotCallTestRunWithPreconditionsAsync()
    {
        var mockExecutor = new Mock<IExecutorForPresentation>(MockBehavior.Strict);
        mockExecutor.Setup(e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()))
                    .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunSingleAsync(mockExecutor.Object);

        // MockBehavior.Strict will fail the test if TestRunWithPreconditionsAsync is called.
        mockExecutor.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Once);
    }

    // ── RunSingleTrainedAsync ─────────────────────────────────────────────────

    /// <summary>
    /// Non-console path for <c>SingleTrainedAISimulation</c>: Program.cs creates an
    /// <see cref="IStandardExecutor"/> and calls <see cref="SingleRunner.RunSingleTrainedAsync"/>.
    /// The runner must delegate directly to <see cref="IExecutor.RunAsync()"/>.
    /// </summary>
    [TestMethod]
    public async Task RunSingleTrainedAsync_CallsRunAsyncOnStandardExecutor()
    {
        var mockExecutor = new Mock<IStandardExecutor>();
        mockExecutor.Setup(e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()))
                    .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunSingleTrainedAsync(mockExecutor.Object);

        mockExecutor.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Once,
            "RunSingleTrainedAsync must call RunAsync() exactly once on the standard executor");
    }

    // ── RunTestPreconditionsAsync ─────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="SingleRunner.RunTestPreconditionsAsync"/> calls
    /// <see cref="IExecutorForPresentation.TestRunWithPreconditionsAsync()"/> and not
    /// <see cref="IExecutor.RunAsync()"/>.
    /// </summary>
    [TestMethod]
    public async Task RunTestPreconditionsAsync_CallsTestRunWithPreconditionsAsync()
    {
        var mockExecutor = new Mock<IExecutorForPresentation>();
        mockExecutor.Setup(e => e.TestRunWithPreconditionsAsync())
                    .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunTestPreconditionsAsync(mockExecutor.Object);

        mockExecutor.Verify(e => e.TestRunWithPreconditionsAsync(), Times.Once,
            "RunTestPreconditionsAsync must delegate to TestRunWithPreconditionsAsync");
        mockExecutor.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Never,
            "RunAsync must NOT be called from RunTestPreconditionsAsync");
    }

    // ── Contract: console-path executor publishes to the shared broker ────────

    /// <summary>
    /// Documents the contract that separates the two <c>SingleTrainedAISimulation</c>
    /// paths:
    /// <list type="bullet">
    ///   <item>
    ///     <see cref="IExecutorForPresentation"/> (console path) uses the shared
    ///     <c>IMessageBroker</c> singleton, so <c>ConsoleRunner</c> subscribers receive events.
    ///   </item>
    ///   <item>
    ///     <see cref="IStandardExecutor"/> (non-console path) owns a private broker,
    ///     so its events are invisible to any <c>ConsoleRunner</c>.
    ///   </item>
    /// </list>
    /// Both paths call <c>RunAsync()</c> on their respective executor.
    /// This test asserts the correct method is invoked in both scenarios.
    /// </summary>
    [TestMethod]
    public async Task SingleTrainedAISimulation_BothPaths_CallRunAsync()
    {
        // Console path — IExecutorForPresentation (shared broker, ConsoleRunner receives events)
        var consoleMock = new Mock<IExecutorForPresentation>();
        consoleMock.Setup(e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()))
                   .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunSingleAsync(consoleMock.Object);

        consoleMock.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Once,
            "Console path: IExecutorForPresentation.RunAsync() must be called once");

        // Non-console path — IStandardExecutor (private broker, no console rendering)
        var standardMock = new Mock<IStandardExecutor>();
        standardMock.Setup(e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()))
                    .Returns(Task.CompletedTask);

        await new SingleRunner(_config).RunSingleTrainedAsync(standardMock.Object);

        standardMock.Verify(
            e => e.RunAsync(It.IsAny<Guid>(), It.IsAny<SandBoxConfiguration>()),
            Times.Once,
            "Non-console path: IStandardExecutor.RunAsync() must be called once");
    }
}
