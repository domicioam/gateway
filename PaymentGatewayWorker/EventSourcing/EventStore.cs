﻿using MediatR;
using Microsoft.Extensions.Logging;
using PaymentGatewayWorker.CQRS.CommandStack;
using PaymentGatewayWorker.CQRS.CommandStack.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PaymentGatewayWorker.EventSourcing
{
    class EventStore : IEventStore, 
        INotificationHandler<PaymentCreatedEvent>,
        IRequestHandler<PaymentSentForBankApprovalEvent>

    {
        private EventRepository _eventRepository;
        private ILogger<EventStore> _logger;

        public async Task SaveAsync<T>(T @event) where T : Event
        {
            var loggedEvent = new LoggedEvent()
            {
                Action = @event.Name,
                AggregateId = @event.AggregateId,
                Data = JsonSerializer.Serialize(@event)
            };

            try
            {
                await _eventRepository.SaveAsync(loggedEvent);
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error while trying to save logged event.");
            }
        }

        public async Task Handle(PaymentCreatedEvent notification, CancellationToken cancellationToken)
        {
            await SaveAsync<Event>(notification);
        }

        public async Task<Unit> Handle(PaymentSentForBankApprovalEvent request, CancellationToken cancellationToken)
        {
            await SaveAsync<Event>(request);
            return Unit.Value;
        }

        public EventStore(EventRepository eventRepository, ILogger<EventStore> logger)
        {
            _eventRepository = eventRepository;
            _logger = logger;
        }
    }
}
