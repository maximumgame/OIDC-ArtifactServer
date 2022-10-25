using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace OIDC_ArtifactServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        public AccountController()
        {
        }

        [Authorize]
        [HttpGet("signout")]
        public async Task Logout()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                return;
            var prop = new AuthenticationProperties()
            {
                RedirectUri = "/"
            };
            await HttpContext.SignOutAsync("Cookies");
            await HttpContext.SignOutAsync("OpenIdConnect", prop);
        }

        [Authorize]
        [HttpGet("signin")]
        public async Task<IActionResult> Signin()
        {
            return Redirect("/");
        }
    }
}
