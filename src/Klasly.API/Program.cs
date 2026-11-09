using System.Text;
using System.Text.Json.Serialization;
using Klasly.API.Middleware;
using Klasly.API.Services;
using Klasly.API.Services.Interfaces;
using Klasly.Core.Entities;
using Klasly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Enums travel as names ("Student", "Dropped"), the same strings the database stores; numbers are rejected.
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
{
    // Bearer scheme so Swagger UI shows an "Authorize" button and sends the JWT on every call.
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
    {
        ["Bearer"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }
    };
    document.Security = [new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] }];
    return Task.CompletedTask;
}));

// EF Core context; also the design-time target of dotnet ef migrations.
builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Klasly")
    ?? throw new InvalidOperationException("Connection string 'Klasly' is missing."));

// JWT: the key lives in user-secrets only (guide Section 14). HS256 needs at least 256 bits.
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Run: " +
        "dotnet user-secrets set \"Jwt:Key\" \"<at least 32 random characters>\" --project src/Klasly.API");

// QR signing key: user-secrets only (CLAUDE.md §3.3). Same minimum as the JWT key.
var qrKey = builder.Configuration["Qr:SigningKey"];
if (string.IsNullOrWhiteSpace(qrKey) || qrKey.Length < 32)
    throw new InvalidOperationException(
        "Qr:SigningKey is missing or shorter than 32 characters. Run: " +
        "dotnet user-secrets set \"Qr:SigningKey\" \"<at least 32 random characters>\" --project src/Klasly.API");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the short JWT claim names (sub, role, name) as issued
        options.Events = new JwtBearerEvents
        {
            // Missing or expired token: a problem-details body the Android app can show, instead of an empty 401.
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>()
                    .WriteAsync(new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status401Unauthorized,
                            Detail = "Session expired – please log in"
                        }
                    });
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("TeacherOnly", p => p.RequireRole("Teacher"))
    .AddPolicy("StudentOnly", p => p.RequireRole("Student"))
    .AddPolicy("ParentOnly", p => p.RequireRole("Parent"));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ITermService, TermService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Klasly API v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
