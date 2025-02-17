﻿using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using ProtoBuf.Grpc.Configuration;
using ProtoBuf.Grpc.Server;
using Serilog;
using Slate.Backend.Shared;
using Slate.GameWarden.Middleware;
using Slate.GameWarden.ServiceLocation;
using Slate.Networking.External.Protocol.Services;

namespace Slate.GameWarden
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCoreSlateServices<GameContainer>(_configuration);

            Log.Logger.Information("GameWarden Starting");

            services.UseStrongInjectForGrpcServiceResolution();
            services.AddCodeFirstGrpc(config =>
            {
                config.ResponseCompressionLevel = CompressionLevel.Optimal;
                config.EnableDetailedErrors = true;
            });
            services.TryAddSingleton(BinderConfiguration.Create(binder: new ServiceBinderWithServiceResolutionFromServiceCollection(services)));
            services.AddCodeFirstGrpcReflection();

            services.AddAuthorization();
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "https://localhost:8001";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false
                    };
                });

            services.ReplaceWithSingletonServiceUsingContainer<GameContainer, IAuthorizationService>();
            services.ReplaceWithSingletonServiceUsingContainer<GameContainer, IAccountService>();
            services.ReplaceWithSingletonServiceUsingContainer<GameContainer, IGameService>();

            services.AddHostedServiceUsingContainer<GameContainer, MonitorEventBus>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment _)
        {
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // custom jwt auth middleware
            app.UseMiddleware<JwtMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<IAuthorizationService>();
                endpoints.MapGrpcService<IAccountService>();
                endpoints.MapGrpcService<IGameService>();
            });
        }
    }
}

