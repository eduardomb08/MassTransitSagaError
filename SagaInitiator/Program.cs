using MassTransit;
using Messages;
using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Messages
{
    public interface ISagaRequest : CorrelatedBy<Guid> { }
}

namespace SagaInitiator
{
    class Program
    {
        private readonly static string BASE_URI = "rabbitmq://localhost:5672";

        static async Task Main(string[] args)
        {
            IBusControl bus = ConfigureBus();

            using var cancelation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await bus.StartAsync(cancelation.Token);

            try
            {
                while (true)
                {
                    await Console.Out.WriteLineAsync("Press any key to create a quote...");
                    await Task.Run(Console.ReadKey);

                    await bus.Publish<ISagaRequest>(new { CorrelationId = "71074599-9C3F-9A0B-08F7-D5836034C9F2" });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                try
                {
                    bus.Stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        private static IBusControl ConfigureBus()
        {
            var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri(BASE_URI), c =>
                {
                    c.Username("guest");
                    c.Password("guest");

                    //c.UseSsl(ssl =>
                    //{
                    //    ssl.ServerName = "*.Amwins.net";
                    //    ssl.Protocol = SslProtocols.Tls12;
                    //});
                });
            });
            return bus;
        }
    }
}
