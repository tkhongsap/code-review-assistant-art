using Argento.ReportingService.Extensions;
using Argento.ReportingService.FilterAttributes;
using Argento.ReportingService.Repository.ReportingServiceDB;
using Argento.ReportingService.Utility;
using Argento.ReportingService.Utility.Extensions;
using Arcadia.CrudController.AspNetCore;
using Arcadia.Extensions.DependencyInjection;
using Arcadia.Repository.EFCore;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Text.Json;
using System.Reflection;
using Arcadia.Security.Signature;
using Arcadia.Security.Signature.Enums;
using Microsoft.Extensions.Options;
using Argento.ReportingService.Repository;
using Arcadia.CrudController;
using System.Text.Json.Serialization;
using Argento.ReportingService.ServiceOptions;
using Argento.ReportingService.AutoMapper;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Argento.ReportingService.DL;
using Argento.ReportingService.Services;
using Argento.ReportingService.DL.Utils;

namespace Argento.ReportingService
{
    public class Startup
    {
        private readonly string _policyName = "CorsPolicy";
        private readonly IConfigurationRoot configuration;

        public Startup(IWebHostEnvironment env)
        {
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();

            configuration = configurationBuilder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<BuildVersion>(configuration.GetSection("BuildVersion"));

            // kafka

            services.AddHostedService<ReceiveNotifyUserHost>();
            services.AddScoped<IReceiveNotifyUserService, ReceiveNotifyUserService>();

            services.AddHostedService<KafkaHostMerchantIntegrationService>();
            services.AddScoped<IScopedMerchantIntegrationService, KafkaMerchantIntegrationService>();

            services.AddHostedService<KafkaHostMerchantServiceTypeIntegrationService>();
            services.AddScoped<IScopedMerchantServiceTypeIntegrationService, KafkaMerchantServiceTypeIntegrationService>();

            services.AddHostedService<KafkaHostCallbackUrlService>();
            services.AddScoped<IScopedCallbackUrlService, KafkaCallbackUrlService>();

            services.AddHostedService<KafkaHostAccountIntegrationService>();
            services.AddScoped<IScopedAccountIntegrationService, KafkaAccountIntegrationService>();

            services.AddHostedService<KafkaHostUserRoleIntegrationService>();
            services.AddScoped<IScopedUserRoleIntegrationService, KafkaUserRoleIntegrationService>();

            services.AddScoped<EmailAttachment>();

            services.RegisterDependenciesByAttribute(Assembly.Load("Argento.ReportingService"))
                .RegisterDependenciesByAttribute(Assembly.Load("Argento.ReportingService.BL"))
                .RegisterDependenciesByAttribute(Assembly.Load("Argento.ReportingService.Repository.ReportingServiceDB"))
                .RegisterDependenciesByAttribute(Assembly.Load("Argento.ReportingService.Utility"));

            #region HTTP

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            #endregion

            #region Configuration

            services.AddOptions();
            services.Configure<AppSettings>(configuration);
            AppSettings appSettings = configuration.Get<AppSettings>();

            #endregion

            #region CrudController

            var crudControllerConfiguration = new CrudControllerConfiguration();
            services.AddSingleton(crudControllerConfiguration);

            #endregion

            #region Database

            services.AddDbContext<DbContextReportingServiceDB>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            var repositoryConfiguration = new RepositoryConfiguration();

            services.AddSingleton(repositoryConfiguration);

            #endregion

            #region AutoMapper

            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            #endregion

            #region Signature Hasher

            var signatureConfiguration = new SignatureConfiguration();
            services.AddSingleton(signatureConfiguration);

            services.AddSingleton(typeof(ISignatureHasher), sp =>
            {
                AppSettings appSettings = sp.GetService<IOptions<AppSettings>>().Value;

                var config = sp.GetService<SignatureConfiguration>();
                config.Algorithm = SignatureAlgorithm.HMACSHA256;
                config.SignatureKey = appSettings.EncryptionKey;

                var signatureHasher = new SignatureHasher(config);
                return signatureHasher;
            });

            #endregion

            #region Swagger UI

            services.AddSwaggerGen(options =>
            {
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Argento.ReportingService",
                    Version = "v1",
                    Description = $"{appSettings.EnvironmentName} v.{version}"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            #endregion

            #region Mvc Config
            services.AddScoped<ValidationFilterAttribute>();
            services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

            //services.AddMvc(options =>
            //{
            //    options.Filters.Add<ExceptionHandlerFilterAttribute>();

            //    options.ReturnHttpNotAcceptable = true;
            //    options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
            //    options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
            //    options.EnableEndpointRouting = false;
            //}).AddJsonOptions(options =>
            //{
            //    options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter(appSettings.JsonSerializerOptionsDateTimeFormat));
            //}).SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
            services.AddMvc(options =>
            {
                options.Filters.Add<ExceptionHandlerFilterAttribute>();
                //options.Filters.Add<ValidateModelAttribute>();
                options.EnableEndpointRouting = false;
            }).AddJsonOptions(x =>
            {
                x.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter(appSettings.JsonSerializerOptionsDateTimeFormat));
                x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            #endregion

            services.AddCors(opt =>
            {
                opt.AddPolicy(name: _policyName, builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            try
            {
                if (env.EnvironmentName.ToLower() != "production")
                {
                    app.UseDeveloperExceptionPage();
                    app.UseHttpLogging();

                    // arcadia web logging
                    //app.UseWebLogging();

                    app.UseSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Argento.ReportingService");
                    });
                }

                //app.UseWebLogging();

                //app.UseExceptionHandler(a => a.Run(async context =>
                //{
                //    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                //    var exception = exceptionHandlerPathFeature.Error;

                //    ILogger logger = loggerFactory.CreateLogger(typeof(ArcadiaConstants.LoggerNames.Error).Name);
                //    logger.LogError(exception.ToErrorLogs());
                //    context.ResponseErrorHeaders(exception, out string error);
                //    context.Response.ContentType = "application/json";
                //    var result = error;

                //    await context.Response.WriteAsync(result);
                //}));

                //app.UseSwagger();
                //app.UseSwaggerUI(options =>
                //{
                //    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Argento.ReportingService");
                //});

                app.UseCors(_policyName);

                app.UseMvc();
            }
            catch (Exception ex)
            {
                ILogger logger = loggerFactory.CreateLogger(typeof(ArcadiaConstants.LoggerNames.Error).Name);
                logger.LogCritical(ex.ToErrorLogs());

                throw;
            }
        }
    }
}
