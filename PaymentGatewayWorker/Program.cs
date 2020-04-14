using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CQRS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PaymentGatewayWorker.EventSourcing;
using Microsoft.EntityFrameworkCore;
using PaymentGatewayWorker.Mapper;
using PaymentGatewayWorker.Domain.Services;
using PaymentGatewayWorker.Domain.Payments.Data;
using Microsoft.Extensions.Options;
using PaymentGatewayWorker.Domain.Payments.Data.Repository;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using MediatR;
using PaymentGatewayWorker.Domain.Payments.Services;

namespace PaymentGatewayWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var rabbitMqConfig = hostContext.Configuration.GetSection("rabbitMq");

                    var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
                    optionsBuilder.UseNpgsql(hostContext.Configuration.GetConnectionString("PostgresConnectionString"));
                    services.AddTransient(d => new PaymentsDbContext(optionsBuilder.Options));

                    services.Configure<MongoDbSettings>(hostContext.Configuration.GetSection(nameof(MongoDbSettings)));
                    services.AddSingleton(sp => sp.GetRequiredService<IOptions<MongoDbSettings>>().Value);

                    services.Configure<RabbitMqConfig>(rabbitMqConfig);
                    services.AddTransient<RabbitMqConsumer>();
                    services.AddTransient<IEventStore, EventStore>();
                    services.AddTransient<EventRepository>();
                    services.AddTransient<ProcessPaymentAppService>();
                    services.AddTransient<PaymentService>();
                    services.AddTransient<PaymentRepository>();
                    services.AddHostedService<Worker>();
                    services.AddTransient<IRepository, PaymentRepository>();
                    services.AddTransient<BankService>();

                    services.AddMediatR(typeof(Program));

                    var mapperConfig = MapperConfigurationFactory.MapperConfiguration;
                    services.AddSingleton(mapperConfig.CreateMapper());
                });
    }
}
