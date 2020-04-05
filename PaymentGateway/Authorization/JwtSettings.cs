﻿namespace PaymentGateway.Authorization
{
    public class JwtSettings
    {
        public string PrivateKeyXML { get; set; }
        public string PublicKeyXML { get; set; }
        public string Issuer { get; set; }
    }
}