﻿using CQRS;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentGatewayWorker.CQRS
{
    public class AddNewPaymentCommand : IRequest
    {
        public string Name { get; }
        public Guid UserId { get; set; }
        public Guid AggregateId { get; }
        public string CardNumber { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; }
        public string CVV { get; set; }

        public AddNewPaymentCommand(Guid userId, Guid id, string cardNumber, int expiryMonth, int expiryYear, decimal amount, string currencyCode, string cvv)
        {
            Name = "AddNewPayment";
            UserId = userId;
            AggregateId = id;
            CardNumber = cardNumber;
            ExpiryMonth = expiryMonth;
            ExpiryYear = expiryYear;
            Amount = amount;
            CurrencyCode = currencyCode;
            CVV = cvv;
        }
    }
}
