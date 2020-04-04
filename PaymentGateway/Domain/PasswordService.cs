﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace PaymentGateway.Domain
{
    internal static class PasswordService
    {
        public static Password GenerateHashedPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hashed = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                10000,
                256 / 8);

            return new Password { Hash = Convert.ToBase64String(hashed), Salt = Convert.ToBase64String(salt) };
        }

        public static bool IsPasswordValid(string password, string storedHash, string storedSalt)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);

            byte[] hashed = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                10000,
                256 / 8);

            string hashedPassword = Convert.ToBase64String(hashed);
            return hashedPassword == storedHash;
        }
    }
}
