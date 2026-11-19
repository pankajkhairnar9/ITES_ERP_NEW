using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using MyERP.Application;
using MyERP.Domain;
using MyERP.Infrastructure;
using MyERP.Web.Services;

namespace MyERP.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("Erp");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Configure the ConnectionStrings:Erp SQL Server connection before starting MyERP.");

            services.AddErpInfrastructure(connectionString);
            services.AddScoped<WorkflowService>();
            services.AddScoped<IPasswordHasher<ErpUser>, PasswordHasher<ErpUser>>();
            services.AddScoped<IErpDatabaseInitializer, ErpDatabaseInitializer>();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Cookie.Name = "MyERP.Auth";
                    options.Cookie.HttpOnly = true;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                });
            services.AddAuthorization(options =>
            {
                foreach (var permission in new[]
                {
                    "dashboard.view", "masters.manage", "admin.manage", "purchase.request",
                    "purchase.approve", "purchase.order", "purchase.order.approve", "grn.manage",
                    "sales.manage", "stock.adjust", "reports.view"
                })
                    options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
            });
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            if (Configuration.GetValue<bool>("Database:InitializeOnStartup"))
            {
                using (var scope = app.ApplicationServices.CreateScope())
                    scope.ServiceProvider.GetRequiredService<IErpDatabaseInitializer>().InitializeAsync().GetAwaiter().GetResult();
            }

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
