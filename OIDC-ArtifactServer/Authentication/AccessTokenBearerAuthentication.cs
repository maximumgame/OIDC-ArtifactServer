using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using OIDC_ArtifactServer.Models;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OIDC_ArtifactServer.Authentication
{
    public class AccessTokenBearerAuthentication : AuthenticationSchemeOptions
    {
        public const string DefaultSchemeName = "AccessTokenBearerScheme";
        public string TokenHeaderName { get; set; } = "Authorization";
    }

    public class AccessTokenBearerAuthenticationHandler : AuthenticationHandler<AccessTokenBearerAuthentication>
    {
        private readonly ILogger<AccessTokenBearerAuthenticationHandler> _logger;
        private readonly ArtifactDb db;

        public AccessTokenBearerAuthenticationHandler(IOptionsMonitor<AccessTokenBearerAuthentication> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ArtifactDb artifactDb)
            : base(options, logger, encoder, clock)
        {
            _logger = logger.CreateLogger<AccessTokenBearerAuthenticationHandler>();
            db = artifactDb;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(Options.TokenHeaderName))
                return Task.FromResult(AuthenticateResult.Fail($"Missing Header For Token: {Options.TokenHeaderName}"));

            string token = Request.Headers[Options.TokenHeaderName];

            if(!token.Trim().Contains(' '))
                return Task.FromResult(AuthenticateResult.Fail("Invalid token format"));

            string tokenValue = token.Split(' ', StringSplitOptions.None)[1];

            //TODO validate tokenValue with db
            if (!TokenInDb(tokenValue))
                return Task.FromResult(AuthenticateResult.Fail("Token is not accepted"));

            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, tokenValue),
                new Claim(ClaimTypes.Name, tokenValue)
            };

            var id = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(id);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        private bool TokenInDb(string token)
        {
            return db.AccessTokens.Any(x => x.Token == token);
        }
    }
}
