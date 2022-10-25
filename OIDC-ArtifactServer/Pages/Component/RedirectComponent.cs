using Microsoft.AspNetCore.Components;

namespace OIDC_ArtifactServer.Pages.Component
{
    public class RedirectComponent : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        IHttpContextAccessor Context { get; set; }

        protected override void OnInitialized()
        {
            if(!this.Context.HttpContext.User.Identity.IsAuthenticated)
            {
                var challengeUri = "/api/Account/signin";
                this.NavigationManager.NavigateTo(challengeUri, true);
            }
        }
    }
}
