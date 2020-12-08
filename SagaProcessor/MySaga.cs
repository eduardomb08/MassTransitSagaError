using MassTransit;
using MassTransit.Saga;
using Messages;
using System;
using System.Threading.Tasks;

namespace Messages
{
    public interface ISagaRequest : CorrelatedBy<Guid> { }
}

namespace SagaProcessor
{ 
    public class MySaga : ISaga,
            InitiatedBy<ISagaRequest>
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; }

        public async Task Consume(ConsumeContext<ISagaRequest> context)
        {
            if (CurrentState == "Error")
            {
                await Console.Out.WriteLineAsync($"Fixing saga {context.Message.CorrelationId}");
                CurrentState = "Success";
                return;
            }

            if (CurrentState != "Success")
            {
                await Console.Out.WriteLineAsync($"Creating saga {context.Message.CorrelationId}");
                CurrentState = "Error";
            }
        }
    }
}
