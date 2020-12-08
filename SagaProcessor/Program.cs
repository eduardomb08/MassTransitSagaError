using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using MassTransit.EntityFrameworkCoreIntegration.Mappings;
using MassTransit.EntityFrameworkCoreIntegration.Saga;
using MassTransit.Saga;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SagaProcessor
{
    class Program
    {
        private readonly static string BASE_URI = "rabbitmq://localhost:5672";
        private readonly static string DB_CS = @"data source=localhost\sql2k17;initial catalog=MovieList;Integrated Security=true";

        static async Task Main(string[] args)
        {
            var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                cfg.Host(new Uri(BASE_URI), c =>
                {
                    c.Username("guest");
                    c.Password("guest");
                });

                cfg.ReceiveEndpoint("mysaga", ec =>
                {
                    ec.UseInMemoryOutbox();
                    
                    var _mySagaInstance = new MySaga();
                    //ISagaRepository<MySaga> repoCreateQuoteSaga = new InMemorySagaRepository<MySaga>();

                    ISagaRepository<MySaga> repoCreateQuoteSaga = EntityFrameworkSagaRepository<MySaga>.CreatePessimistic(() =>
                   {
                       var options = new DbContextOptionsBuilder()
                           .UseSqlServer(DB_CS, m =>
                           {
                               object p = m.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                               m.MigrationsHistoryTable($"__MySaga");
                           })
                           .Options;

                       var dbContext = new MySagaDbContext(options);
                       return dbContext;
                   });
                    ec.Saga(repoCreateQuoteSaga);
                });
            });

            using var cancelation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await bus.StartAsync(cancelation.Token);

            try
            {
                await Console.Out.WriteLineAsync("Press any key to exit...");
                await Task.Run(Console.ReadKey);
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
    }

    public class MySagaMap : SagaClassMap<MySaga>
    {
        protected override void Configure(EntityTypeBuilder<MySaga> entity, ModelBuilder model)
        {
            entity.ToTable("tbMySaga");
            entity.Property(x => x.CurrentState).HasMaxLength(64);
        }
    }

    public class MySagaDbContext : SagaDbContext
    {
        public MySagaDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override IEnumerable<ISagaClassMap> Configurations
        {
            get { yield return new MySagaMap(); }
        }
    }
}
