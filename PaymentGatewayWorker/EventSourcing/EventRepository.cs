﻿using Microsoft.EntityFrameworkCore;
using PaymentGatewayWorker.CQRS.CommandStack.Events;
using PaymentGatewayWorker.Domain.Payments.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentGatewayWorker.EventSourcing
{
    class EventRepository
    {
        private readonly PaymentsDbContext _dbContext;

        public async Task SaveAsync(LoggedEvent @event)
        {
            @event.TimeStamp = DateTime.UtcNow;
            _dbContext.LoggedEvents.Add(@event);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IList<LoggedEvent>> AllAsync(Guid aggregateId)
        {
            var events = _dbContext.LoggedEvents.Where(e => e.AggregateId == aggregateId);
            return await events.ToListAsync();
        }

        internal virtual async Task<bool> IsNotApprovedOrDenied(Guid id)
        {
            string actionName = nameof(PaymentAcceptedEvent);
            bool any = await _dbContext.LoggedEvents.AnyAsync(x => x.Action == actionName && x.Id == id);
            return !any;
        }

        public EventRepository(PaymentsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Necessary for mocking
        protected EventRepository()
        {

        }
    }
}
