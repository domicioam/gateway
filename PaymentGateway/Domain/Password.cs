﻿namespace PaymentGateway.Domain
{
    internal class Password
    {
        public string Hash { get; set; }
        public string Salt { get; set; }
    }
}