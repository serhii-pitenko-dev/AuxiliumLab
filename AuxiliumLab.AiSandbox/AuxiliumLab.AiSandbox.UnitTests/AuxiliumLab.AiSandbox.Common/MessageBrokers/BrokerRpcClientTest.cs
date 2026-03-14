using AuxiliumLab.AiSandbox.Common.MessageBroker;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.AiContract.Responses;
using AuxiliumLab.AiSandbox.Common.MessageBroker.Contracts.CoreServicesContract.Events;
using AuxiliumLab.AiSandbox.SharedBaseTypes.MessageTypes;

namespace AuxiliumLab.AiSandbox.UnitTests.AuxiliumLab.AiSandbox.Common.MessageBrokers;

[TestClass]
public class BrokerRpcClientTest
{
    IMessageBroker _messageBroker = new MessageBroker();

    IBrokerRpcClient _brokerRpcClient = default;

    [TestInitialize]
    public void Initialize()
    {
        _brokerRpcClient = new BrokerRpcClient(_messageBroker);
    }

    [TestMethod]
    public async Task BrokerRpcClient_PublishWithResponse_Success()
    {
        _messageBroker.Subscribe<InitialEvent>(msg =>
        {
            _messageBroker.Publish(new ResponseMessage(Guid.NewGuid(), msg.PlaygroundId, msg.Id));
        });

        var result =
            await _brokerRpcClient.RequestAsync<InitialEvent, ResponseMessage>(new InitialEvent(Guid.NewGuid(), Guid.NewGuid()));
    }


    private record InitialEvent(Guid Id, Guid PlaygroundId) : Event(Id);

    private record ResponseMessage(Guid Id, Guid PlaygroundId, Guid CorrelationId) : Response(Id, CorrelationId);

}