﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aiursoft.API.Data;
using Aiursoft.API.Models;
using Aiursoft.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Services;
using Microsoft.Extensions.Hosting;
using Aiursoft.Pylon.Services.ToDeveloperServer;
using Aiursoft.Pylon.Services.ToOSSServer;

namespace Aiursoft.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<APIDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DatabaseConnection")));

            services.AddIdentity<APIUser, IdentityRole>(options => options.Password = Values.PasswordOptions)
                .AddEntityFrameworkStores<APIDbContext>()
                .AddDefaultTokenProviders();

            services.AddTokenManager();

            services
                .AddLocalization(options => options.ResourcesPath = "Resources");

            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();

            services.AddSingleton<IHostedService, TimedCleaner>();
            services.AddSingleton<ServiceLocation>();
            services.AddScoped<HTTPService>();
            services.AddScoped<DeveloperApiService>();
            services.AddScoped<GrantChecker>();
            services.AddScoped<OSSApiService>();
            services.AddTransient<UserImageGenerator<APIUser>>();
            services.AddTransient<AiurEmailSender>();
            services.AddTransient<APISMSSender>();
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseHandleRobots();
                app.UseEnforceHttps();
                app.UseExceptionHandler("/Error/ServerException");
                app.UseStatusCodePagesWithReExecute("/Error/Code{0}");
            }
            app.UseAiursoftSupportedCultures();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseLanguageSwitcher();
            app.UseMvcWithDefaultRoute();
            app.UseDocGenerator();
        }
    }
}
