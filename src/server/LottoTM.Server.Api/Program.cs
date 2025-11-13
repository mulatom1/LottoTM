using FluentValidation;
using LottoTM.Server.Api.Middlewares;
using LottoTM.Server.Api.Repositories;
using LottoTM.Server.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

var connStrBase64 = builder.Configuration.GetConnectionString("DefaultConnection");
var connStr = Encoding.UTF8.GetString(Convert.FromBase64String(connStrBase64 ?? throw new InvalidOperationException("Connection string not configured")))
        .Replace("\\\\","\\");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connStr));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

// Register services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IXLottoService, XLottoService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
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
            new string[] {}
        }
    });
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();


if (builder.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseSerilogRequestLogging();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();


// Register endpoints
LottoTM.Server.Api.Features.ApiVersion.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.Login.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.Register.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Auth.SetAdmin.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.DrawsCreate.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.DrawsGetList.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.DrawsGetById.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.DrawsUpdate.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Draws.DrawsDelete.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.TicketsCreate.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.TicketsGetList.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.TicketsGetById.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.TicketsUpdate.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.TicketsDelete.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.GenerateRandom.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Tickets.GenerateSystem.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.Verification.Check.Endpoint.AddEndpoint(app);
LottoTM.Server.Api.Features.XLotto.ActualDraws.Endpoint.AddEndpoint(app);


await app.RunAsync();

// Make the implicit Program class public so test projects can access it
public partial class Program { }