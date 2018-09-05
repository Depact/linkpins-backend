﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BackSide2.BL.Entity;
using BackSide2.DAO.Entities;
using BackSide2.DAO.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BackSide2.BL.authorize
{
    [Serializable]
    public class TokenServiceException : ApplicationException
    {
        public TokenServiceException()
        {
        }

        public TokenServiceException(string message) : base(message)
        {
        }

        public TokenServiceException(string message, Exception ex) : base(message)
        {
            Ex = ex;
        }

        // Конструктор для обработки сериализации типа
        protected TokenServiceException(SerializationInfo info,
            StreamingContext contex)
            : base(info, contex)
        {
        }

        public Exception Ex { get; }
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IRepository<Person> _personService;

        public TokenService(
            IConfiguration configuration,
            IRepository<Person> personService
        )
        {
            _configuration = configuration;
            _personService = personService;
        }


        public async Task<object> RegisterAsync(RegisterDto model)
        {
            var newbieRole = Role.User;

            Person person =
                await (await _personService.GetAllAsync(d => d.Email == model.Email || d.UserName == model.Username))
                    .FirstOrDefaultAsync();

            if (person != null)
            {
                if (person.UserName == model.Username && person.Email == model.Email)
                    throw new TokenServiceException("Email and username already taken.");

                if (person.UserName == model.Username) throw new TokenServiceException("Username already taken.");

                if (person.Email == model.Email) throw new TokenServiceException("Email already taken.");
            }

            Person personToRegister = new Person
            {
                UserName = model.Username,
                Email = model.Email,
                Password = Hash.GetPassHash(model.Password),
                Role = newbieRole,
                FirstName = model.FirstName,
                Surname = model.Surname,
                Gender = null,
                Language = null,
                CreatedBy = null,
                UpdatedBy = null
            };

            var systemUser = await (await _personService.GetAllAsync(d => d.UserName == "system"))
                .FirstOrDefaultAsync();
            if (systemUser != null)
                personToRegister.CreatedBy = systemUser.Id;
            else
                throw new TokenServiceException("SystemUserNotFound");

            await _personService.InsertAsync(personToRegister);
            return new
            {
                token = GenerateJwtToken(model.Username, newbieRole.ToString(), model.Username),
                username = personToRegister.UserName,
                email = personToRegister.Email,
                Role = personToRegister.Role.ToString(),
            };
            //GenerateJwtToken(model.Username, newbieRole.ToString(), model.Username);
        }

        public async Task<object> LoginAsync(
            LoginDto model
        )
        {
            Person person =
                await (await _personService.GetAllAsync(d =>
                        d.Email == model.Email && d.Password == Hash.GetPassHash(model.Password)))
                    .FirstOrDefaultAsync();
            if (person != null) return new
            {
                token = GenerateJwtToken(person.Email, person.Role.ToString(), person.UserName),
                username = person.UserName,
                email = person.Email,
                Role = person.Role.ToString(),
            };

            throw new TokenServiceException("Wrong email, or password.");
        }

        public async Task<object> GetUserProfileInfo(string email)
        {
            return
                await (await _personService.GetAllAsync(d =>
                        d.Email == email))
                    .FirstOrDefaultAsync();
        }


        private object GenerateJwtToken(
            string email,
            string role,
            string login
        )
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.UniqueName, login),
                new Claim("role" , role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["JwtExpireDays"]));
            var token = new JwtSecurityToken(
                _configuration["JwtIssuer"],
                _configuration["JwtAudience"],
                claims,
                expires: expires,
                signingCredentials: creds
            );
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(token);
            return encodedJwt;
        }
    }
}