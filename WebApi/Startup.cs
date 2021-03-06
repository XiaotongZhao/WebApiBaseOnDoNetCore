﻿using System;
using System.Data;
using System.Linq;
using System.Text;
using Hangfire;
using Hangfire.MySql.Core;
using Infrastructure.Config.IoC;
using Infrastructure.Repository.RepositoryImplement;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApi.FilterAttribute;
using Serilog;
using WebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using WebApi.Middleware;
using Microsoft.OpenApi.Models;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddCors();
            services.AddControllers(option => option.Filters.Add(typeof(HttpGlobalExceptionFilter)))
                .AddNewtonsoftJson();
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("TokenManagement:Secret").Value)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(4)
                };
            });

            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseStorage(new MySqlStorage(
                Configuration.GetConnectionString("DBConnection"),
                    new MySqlStorageOptions
                    {
                        TransactionIsolationLevel = IsolationLevel.ReadCommitted,
                        QueuePollInterval = TimeSpan.FromSeconds(15),
                        JobExpirationCheckInterval = TimeSpan.FromHours(1),
                        CountersAggregateInterval = TimeSpan.FromMinutes(5),
                        PrepareSchemaIfNecessary = true,
                        DashboardJobListLimit = 50000,
                        TransactionTimeout = TimeSpan.FromMinutes(1),
                        TablePrefix = "Hangfire"
                    }
                )));

            services.AddHangfireServer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("WebApi", new OpenApiInfo { Title = "WebApi", Version = "v1" });
            });
            services.AddConsulConfig(Configuration);
            return IoCConfig.ImplementDI(services, Configuration);
        }

        public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetService<EFContext>();
                var pendingMigrations = dbContext.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                    dbContext.Database.Migrate();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            //app.UseRequestCulture();
            app.UseSerilogRequestLogging();
            app.UseCors(x => x
             .AllowAnyOrigin()
             .AllowAnyMethod()
             .AllowAnyHeader());

            app.UseHttpsRedirection();

            app.UseSwagger(option => 
            {
                option.RouteTemplate = "{documentName}/swagger.json";
            });
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/WebApi/swagger.json", "WebApi");

                options.DocumentTitle = "WebApi";
            });
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapGrpcService<GreeterService>();
                endpoints.MapHangfireDashboard();
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });


            app.UseHangfireDashboard();
            backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            app.UseConsul("https://localhost", "5050");
        }
    }
}
