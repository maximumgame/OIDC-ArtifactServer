using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using OIDC_ArtifactServer.Authentication;
using OIDC_ArtifactServer.Configuration;
using OIDC_ArtifactServer.Models;
using Microsoft.EntityFrameworkCore;
using OIDC_ArtifactServer.Browser;
using OIDC_ArtifactServer.Project;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OIDCConfiguration>(builder.Configuration.GetSection("oidc"));
builder.Services.Configure<UserConfiguration>(builder.Configuration.GetSection("userconfiguration"));
builder.Services.Configure<FileStorage>(builder.Configuration.GetSection("FileStorage"));

//binding here since we these early
UserConfiguration userConfig = new UserConfiguration();
builder.Configuration.GetSection("userconfiguration").Bind(userConfig);
OIDCConfiguration oidc = new OIDCConfiguration();
builder.Configuration.GetSection("oidc").Bind(oidc);

builder.Services.AddSingleton<ProjectEventEmitter>();
builder.Services.AddScoped<BrowserStorage>();
builder.Services.AddScoped<UserTimezone>();
builder.Services.AddScoped<ProjectManagement>();

builder.Services.AddEntityFrameworkNpgsql().AddDbContext<ArtifactDb>(opt =>
{
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgresql"));
    opt.UseLazyLoadingProxies();
});

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opt.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(cookie =>
{
    cookie.Cookie.Name = "oidc-artifact.cookie";
    cookie.Cookie.MaxAge = TimeSpan.FromMinutes(1);
    cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    cookie.SlidingExpiration = true;
})
.AddOpenIdConnect(opt =>
{
    opt.SignInScheme = "Cookies";
    opt.Authority = oidc.Authority;
    opt.MetadataAddress = oidc.MetadataAddress;
    opt.ClientId = oidc.ClientId;
    opt.ClientSecret = oidc.ClientSecret;
    opt.GetClaimsFromUserInfoEndpoint = true;
    opt.RequireHttpsMetadata = true;

    foreach(var scope in userConfig.Scopes)
    {
        opt.Scope.Add(scope);
    }
    opt.SaveTokens = true;
    opt.ResponseType = OpenIdConnectResponseType.Code;

    opt.NonceCookie.SameSite = SameSiteMode.Unspecified;
    opt.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

    opt.RequireHttpsMetadata = false;
    opt.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
    {
        NameClaimType = "username",
        RoleClaimType = userConfig.GroupClaimName,
        ValidateIssuer = true
    };

}).AddScheme<AccessTokenBearerAuthentication,AccessTokenBearerAuthenticationHandler>(AccessTokenBearerAuthentication.DefaultSchemeName, opt =>
{
    //defaults
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", policy =>
    {
        policy.RequireClaim(userConfig.GroupClaimName, userConfig.GroupPolicies.AdminGroup);
    });
    options.AddPolicy("developer", policy =>
    {
        policy.RequireClaim(userConfig.GroupClaimName, userConfig.GroupPolicies.DeveloperGroup);
    });
    options.AddPolicy("user", policy =>
    {
        policy.RequireClaim(userConfig.GroupClaimName, userConfig.GroupPolicies.UserGroup);
    });
});

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<CookiePolicyOptions>(opt =>
{
    opt.CheckConsentNeeded = context => false;
});

builder.Services.AddSession(opt =>
{
    opt.IdleTimeout = TimeSpan.FromMinutes(15);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    RequireHeaderSymmetry = false,
    ForwardedHeaders = ForwardedHeaders.All
});

if(!app.Environment.IsDevelopment()) //https override on prod
    app.UseHttpMethodOverride();

app.Use(async delegate (HttpContext Context, Func<Task> Next)
{
    //https://stackoverflow.com/a/71446650
    //this throwaway session variable will "prime" the SetString() method
    //to allow it to be called after the response has started
    var TempKey = Guid.NewGuid().ToString(); //create a random key
    Context.Session.Set(TempKey, Array.Empty<byte>()); //set the throwaway session variable
    Context.Session.Remove(TempKey); //remove the throwaway session variable

    await Next(); //continue on with the request
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapControllers();

//all pages require authorization
app.UseEndpoints(ep =>
{
    ep.MapRazorPages().RequireAuthorization();
});


//create/migrate db here on startup
using(var serviceScope = app.Services.CreateScope())
{
    var db = serviceScope.ServiceProvider.GetRequiredService<ArtifactDb>();

    //we try to migrate multiple times in case the database is not ready yet
    int trials = 0;
    bool succeeded = false;
    while(trials < 3 && !succeeded)
    {
        try
        {
            db.Database.Migrate();

            //if we succeed set our break condition
            succeeded = true;
        }
        catch(Exception e)
        {
            trials++;
            Console.WriteLine(e.Message);
            //Maybe the db needs a few moments so we wait a sec
            Thread.Sleep(1000);
        }
    }

    if(!succeeded)
    {
        Console.WriteLine("Exhausted attempts to migrate database... Exiting...");
        return;
    }
}

app.Run();
