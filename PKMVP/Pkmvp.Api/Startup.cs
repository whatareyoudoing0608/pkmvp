using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using Dapper;
using Microsoft.OpenApi.Models;
using Pkmvp.Api.Auth;
using Pkmvp.Api.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Pkmvp.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            Oracle.ManagedDataAccess.Client.OracleConfiguration.BindByName = true;
            OracleConfiguration.BindByName = true;

            services.AddControllers();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        var keyStr = Configuration["Jwt:Key"];
                        var key = Encoding.UTF8.GetBytes(keyStr);

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            NameClaimType = AuthClaimTypes.UserId,
                            RoleClaimType = AuthClaimTypes.Role
                        };
                    });

            services.AddAuthorization();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PKMVP API",
                    Version = "v1"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Bearer {token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        new string[] { }
                    }
                });

                c.SchemaFilter<HideTokenInjectedIdsSchemaFilter>();
            });

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserAccessor, HttpContextCurrentUserAccessor>();

            services.AddSingleton<Pkmvp.Api.Repositories.IWeeklyReportRepository, Pkmvp.Api.Repositories.WeeklyReportRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.ITaskRepository, Pkmvp.Api.Repositories.TaskRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.ITaskProgressRepository, Pkmvp.Api.Repositories.TaskProgressRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.ITaskEvaluationRepository, Pkmvp.Api.Repositories.TaskEvaluationRepository>();
            services.AddSingleton<Pkmvp.Api.Auth.ITokenService, Pkmvp.Api.Auth.TokenService>();
            services.AddSingleton<Pkmvp.Api.Repositories.IAuthRepository, Pkmvp.Api.Repositories.AuthRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.IDailyWorklogRepository, Pkmvp.Api.Repositories.DailyWorklogRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.ITaskCommentRepository, Pkmvp.Api.Repositories.TaskCommentRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.INotificationRepository, Pkmvp.Api.Repositories.NotificationRepository>();
            services.AddSingleton<Pkmvp.Api.Repositories.IPlanningRepository, Pkmvp.Api.Repositories.PlanningRepository>();
            services.AddSingleton<ITeamDirectory, TeamDirectory>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // In local/dev runs we often bind HTTP only; avoid noisy redirect warnings.
            if (!env.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

            // UnauthorizedAccessException -> 403 JSON, etc.
            // Place AFTER DeveloperExceptionPage and BEFORE UseRouting.
            app.UseMiddleware<ApiExceptionMiddleware>();

            app.UseRouting();

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "PKMVP API v1");
                c.RoutePrefix = "swagger";
                c.ConfigObject.AdditionalItems["persistAuthorization"] = "true";
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}




