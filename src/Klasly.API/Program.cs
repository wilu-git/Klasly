using System.Text;
using Klasly.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep the short JWT claim names (sub, role, name) as issued
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
