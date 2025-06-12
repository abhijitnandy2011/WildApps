using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OfficeOpenXml;
using RAppsAPI.Data;
using RAppsAPI.Services;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using Serilog;
using Serilog.Events;


ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("RApps: Just after bootstrap logger");

try 
{
    var builder = WebApplication.CreateBuilder(args);

    if (!builder.Environment.IsDevelopment())
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                System.IO.Path.Combine(Environment.GetEnvironmentVariable("HOME"), "LogFiles", "Application", "diagnostics.txt"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 2,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();
    }
    else
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                "logs\\log.txt",
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 2,
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

        /*builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));*/

        //builder.Logging.AddSerilog();

    }

    Log.Information("RApps: Starting web host...");
    //Log.Logger.Information("RApps: Logger: Starting web host");

    //builder.Host.UseSerilog();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(
            policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader();  // TODO: this should not be needed!
            });
    });

    // Add services to the container.

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
        });
        options.OperationFilter<SecurityRequirementsOperationFilter>();
    });

    builder.Services.AddDbContext<RDBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme; // IdentityConstants.ApplicationScheme;
    }).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Token"]!)),
            ValidateIssuerSigningKey = true
        };
    });


    // Register interfaces for DI
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IFileService, FileService>();
    builder.Services.AddScoped<IFolderService, FolderService>();
    builder.Services.AddScoped<IMPMService, MPMService>();
    builder.Services.AddSingleton<IMPMBackgroundRequestQueue, MPMBackgroundRequestQueue>();
    builder.Services.AddSingleton<IMPMSpreadsheetService, MPMSpreadsheetService>();
    builder.Services.AddSingleton<IMPMBuildCacheService, MPMBuildCacheService>();
    builder.Services.AddHostedService<MPMQueuedReqProcessorBackgroundService>();
    builder.Services.AddHostedService<MPMMonitoringService>();
    //builder.Services.AddLogging();
    builder.Services.AddMemoryCache();

    builder.Services.AddAuthorization();

    var app = builder.Build();



    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch(Exception ex) when (ex.GetType().Name is not "StopTheHostException" &&
                          ex.GetType().Name is not "HostAbortedException")
{
    Log.Fatal(ex, "RApps: Unhandled exception");
}
finally
{
    Log.Information("RApps: Shut down complete");
    Log.CloseAndFlush();
}


