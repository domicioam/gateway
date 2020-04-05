﻿using System;
using System.IO;
using System.Security.Authentication;
using System.Threading.Tasks;
using PaymentGateway.Authorization.Data;

namespace PaymentGateway.Authorization.Services
{
    public class AuthService
    {
        private JwtHandler _jwtHandler;

        private readonly IUserAccountRepository _userAccountRepository;



        public async Task<UserAccount> SaveAsync(string username, string password)
        {
            var hashedPassword = PasswordService.GenerateHashedPassword(password);

            var userAccount = new UserAccount
            {
                Password = hashedPassword.Hash,
                Salt = hashedPassword.Salt,
                Username = username
            };

            await _userAccountRepository.SaveAsync(userAccount);

            return userAccount;
        }

        public async Task<UserJwt> LoginAsync(string userName, string password)
        {
            try
            {
                var user = await _userAccountRepository.GetByUsernameAsync(userName);

                if (!PasswordService.IsPasswordValid(password, user.Password, user.Salt))
                {
                    throw new InvalidCredentialException("Username or password doesn't match!");
                }

                var jwt = _jwtHandler.Create(user.Id.ToString());
                var payload = new UserJwt() { Id = user.Id, Token = jwt.Token, ExpiresIn = jwt.Expires };

                return payload;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public AuthService(JwtHandler jwtHandler, IUserAccountRepository userAccountRepository)
        {
            _jwtHandler = jwtHandler;
            _userAccountRepository = userAccountRepository;
        }

        private async Task<string> ReadStringFromFileAsync(string path)
        {
            if(File.Exists(path))
            {
                return await File.ReadAllTextAsync(path);
            }
            else
            {
                throw new ArgumentException("You need to provide your own .key files.");
            }
        }
    }
}
