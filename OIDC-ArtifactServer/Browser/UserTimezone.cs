using System.Text;

namespace OIDC_ArtifactServer.Browser
{
    public class UserTimezone
    {
        private readonly BrowserStorage _storage;
        private readonly IHttpContextAccessor _context;
        private string? tz;

        public UserTimezone(IHttpContextAccessor context, BrowserStorage storage)
        {
            _storage = storage;
            _context = context;
        }

        /// <summary>
        /// Attempt to retrieve from session first then check browser storage
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetTimezone()
        {
            if(tz == null)
            {
                tz = _context.HttpContext?.Session.GetString("timezone");

                if (tz == null) //now check browser
                {
                    tz = await _storage.RetrieveLocalItem<string>("timezone");
                    if (tz == null)
                    {
                        tz = "UTC";
                    }
                    _context.HttpContext?.Session.SetString("timezone", tz);
                    return tz;
                }
            }

            return tz;
        }

        public async Task SetTimezone(string newTimezone)
        {
            _context.HttpContext?.Session.SetString("timezone", newTimezone);
            await _storage.SetLocalItem<string>("timezone", newTimezone);
            tz = newTimezone;
        }
    }
}
