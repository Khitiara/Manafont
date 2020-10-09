using System;
using Manafont.Config;
using Manafont.Db;
using Manafont.Db.Model;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;

namespace Manafont.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddManafontConfig();
            services.AddManafontDb();
            services.AddHttpClient();
            services.AddAuthentication(options => {
                    options.DefaultAuthenticateScheme = AuthConstants.ApplicationScheme;
                    options.DefaultChallengeScheme = AuthConstants.ApplicationScheme;
                    options.DefaultSignInScheme = AuthConstants.ExternalScheme;
                })
                .AddCookie(AuthConstants.ApplicationScheme, o => {
                    o.LoginPath = new PathString("/Account/Login");
                    o.Events = new CookieAuthenticationEvents {
                        OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                    };
                })
                .AddCookie(AuthConstants.ExternalScheme, o => {
                    o.Cookie.Name = AuthConstants.ExternalScheme;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                })
                .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o => {
                    o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
                    o.Events = new CookieAuthenticationEvents {
                        OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
                    };
                })
                .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o => {
                    o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                    o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                });

            services.AddIdentityCore<ManafontUser>(options => {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ManafontDbContext>()
                .AddDefaultTokenProviders();
            services.AddRazorPages();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddRazorPagesOptions(options => {
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });
            services.Configure<IdentityOptions>(options => {
                options.ClaimsIdentity.UserNameClaimType = OpenIddictConstants.Claims.Name;
                options.ClaimsIdentity.UserIdClaimType = OpenIddictConstants.Claims.Subject;
            });
            services.ConfigureApplicationCookie(options => {
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });
            services.AddHttpContextAccessor();
            services.AddOpenIddict()
                .AddCore(options => {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ManafontDbContext>();
                }).AddServer(options => {
                    options.SetAuthorizationEndpointUris("/oidc/authorize")
                        .SetTokenEndpointUris("/oidc/token");
                    options.AllowAuthorizationCodeFlow()
                        .AllowRefreshTokenFlow();

                    options.RegisterScopes(OpenIddictConstants.Scopes.Email,
                        OpenIddictConstants.Scopes.Profile);

                    options.UseAspNetCore()
                        .EnableStatusCodePagesIntegration()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableLogoutEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableUserinfoEndpointPassthrough()
                        .EnableVerificationEndpointPassthrough();
                }).AddValidation(options => {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });
            services.AddSingleton<IEmailSender, EmailSender>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            } else {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
        }
    }
}