using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentGatewayWorker.CQRS.CommandStack.Commands;
using PaymentGatewayWorker.Domain.Payments.Services;

namespace PaymentGatewayWorker
{
    class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly RabbitMqConsumer _rabbitMqConsumer;
        private readonly IMediator _mediator;
        private readonly SignalRConfig _signalRConfig;
        HubConnection _connection;


        public Worker(ILogger<Worker> logger, RabbitMqConsumer rabbitMqConsumer, IMediator mediator, IOptions<SignalRConfig> signalRConfig)
        {
            _logger = logger;
            _rabbitMqConsumer = rabbitMqConsumer;
            _mediator = mediator;
            _signalRConfig = signalRConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start listening for SignalR Hub
            await StartListeningToSignalRAsync(stoppingToken);

            // Start listening for new payment requests
            _rabbitMqConsumer.StartListeningForPaymentRequests();
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task StartListeningToSignalRAsync(CancellationToken stoppingToken)
        {
            _connection = new HubConnectionBuilder()
                         .WithAutomaticReconnect()
                         .WithUrl(_signalRConfig.ServerUrl)
                         .Build();

            _connection.On<PaymentHubResponse>("PaymentResponse", (response) =>
            {
                _mediator.Send(new UpdatePaymentStatusWithBankResponseCommand(response));
            });

            await ConnectWithRetryAsync(stoppingToken);
        }

        private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    await _connection.StartAsync();
                    Debug.Assert(_connection.State == HubConnectionState.Connected);
                    return;
                }
                catch when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch
                {
                    Debug.Assert(_connection.State == HubConnectionState.Disconnected);
                    await Task.Delay(5000);
                }
            }
        }
    }
}
