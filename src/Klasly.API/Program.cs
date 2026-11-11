using Klasly.API.Push;
using System.Text;
using System.Text.Json.Serialization;
using Klasly.API.Middleware;
using Klasly.API.Services;
using Klasly.API.Services.Interfaces;
using Klasly.Core.Entities;
using Klasly.Core.Interfaces;
using Klasly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog (guide §16): levels from the "Serilog" config section, console only. No request logging, so no bodies or tokens.
builder.Services.AddSerilog((_, logger) => logger.ReadFrom.Configuration(builder.Configuration).WriteTo.Console());

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
            // A deactivated account loses access at once, not when its token expires (soft delete = no more access).
            // Failing here sends the request to OnChallenge below: 401 "Session expired – please log in".
            OnTokenValidated = async context =>
            {
                var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                if (!int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var userId)
                    || !await users.IsActiveAsync(userId, context.HttpContext.RequestAborted))
                    context.Fail("The account is deactivated.");
            },
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
    // Login by default: a controller that forgets [Authorize] is still closed to anonymous callers ([AllowAnonymous] opts out).
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
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
builder.Services.AddScoped<IQrTokenService, QrTokenService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAssessmentService, AssessmentService>();
builder.Services.AddScoped<IOverviewService, OverviewService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Grade push (guide §27.8): saved with the grade, sent in the background through FCM HTTP v1. Fcm:ServiceAccountPath is a user-secret.
builder.Services.Configure<FcmOptions>(builder.Configuration.GetSection("Fcm"));
builder.Services.AddSingleton<FcmCredentials>();
builder.Services.AddHttpClient<IFcmPushClient, FcmPushClient>(client => client.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddSingleton<PushQueue>();
builder.Services.AddScoped<PushNotificationSender>();
builder.Services.AddHostedService<PushSender>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

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

// Lets the tests start the real API in memory (WebApplicationFactory<Program>).
public partial class Program;
