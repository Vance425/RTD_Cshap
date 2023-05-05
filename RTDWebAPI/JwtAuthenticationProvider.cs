using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using RTDWebAPI.Models;
using System.Linq;
using ServiceStack.Configuration;

namespace RTDWebAPI
{
    public class JwtAuthenticationProvider 
    {
        private readonly AppSettings _appSettings;

        // users hardcoded for simplicity, store in a db with hashed passwords in production applications
        private readonly List<LoginUser> _fakeUsers = new List<LoginUser>
        {
            new LoginUser {Id = 1, FirstName = "Test", LastName = "User", Username = "Gyro", Password = "gsi5613686"}
        };

        public JwtAuthenticationProvider(IOptions<AppSettings> appSettings)
        {
            this._appSettings = appSettings.Value;
        }

        public string Authenticate(string userName, string password)
        {
            var user = this._fakeUsers.SingleOrDefault(x => x.Username == userName && x.Password == password);

            // return null if user not found
            if (user == null)
            {
                return null;
            }

            // authentication successful so generate jwt token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("D8AE7CCF-8E64-4843-9CC6-76EBBB87B440");
            var symmetricSecurityKey = new SymmetricSecurityKey(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(JwtRegisteredClaimNames.Sub, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID
 
                //new Claim(ClaimTypes.Name, user.Id.ToString())
            }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials =
                    new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
