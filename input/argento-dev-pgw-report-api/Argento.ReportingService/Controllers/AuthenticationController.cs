using System;
using System.Net;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Argento.ReportingService.DL.Authentications;
using Argento.ReportingService.DL.Users;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Exceptions;

namespace Argento.ReportingService.Controllers
{
    public class AuthenticationController : Controller
    {

        private readonly AppSettings appSettings;

        public AuthenticationController(IOptions<AppSettings> appSettings)
        {
            this.appSettings = appSettings.Value;
        }

        [HttpPost]
        [Route("api/Login")]
        public IActionResult Login([FromBody] LoginParams loginParams)
        {
            if (loginParams.Username != "admin" || loginParams.Password != "Passw0rd")
            {
                throw new ArcadiaException(ArcadiaConstants.ErrorCodes.UsernameOrPasswordWrong, "Username or password is wrong.", 
                    HttpStatusCode.Unauthorized);
            }

            var user = new RequestedUser
            {
                Id = "71bfea8d-9d58-44af-84bf-73e7d192b66a",
                Username = loginParams.Username
            };

            // https://github.com/jwt-dotnet/jwt
            string loginToken = new JwtBuilder()
                .WithAlgorithm(new HMACSHA256Algorithm())
                .WithSecret(appSettings.EncryptionKey)
                .AddClaim("exp", DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds())
                .AddClaim("user", user)
                .Encode();

            return Ok(new LoginInfo
            {
                LoginToken = loginToken,
                RequestedUser = user
            });
        }

    }
}