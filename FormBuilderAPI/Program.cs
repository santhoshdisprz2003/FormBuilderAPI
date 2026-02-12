using FormBuilderAPI.BusinessLogicLayer;
using FormBuilderAPI.DataAccessLayer;
using FormBuilderAPI.Repository;
using FormBuilderAPI.Helper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ===============================
// 1️⃣  CONFIGURATION
// ===============================
var configuration = builder.Configuration;

// ===============================
// 2️⃣  DATABASES
// ===============================

// SQL Server (Entity Framework)
builder.Services.AddDbContext<SQLDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("SqlConnection")));

// MongoDB (custom connection context)
builder.Services.AddSingleton<MongoDbContext>();

// ===============================
// 3️⃣  BUSINESS LOGIC LAYER (Interfaces → Implementations)
// ===============================
builder.Services.AddScoped<IAuthBL, AuthBL>();
builder.Services.AddScoped<IFormBL, FormBL>();
builder.Services.AddScoped<IResponseBL, ResponseBL>();

// Register all repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFormRepository, FormRepository>();
builder.Services.AddScoped<IResponseRepository, ResponseRepository>();



// builder.Services.AddScoped<IAuditBL, AuditBL>();
// ===============================
// 4️⃣  HELPERS
// ===============================
builder.Services.AddSingleton<JwtHelper>();
builder.Services.AddSingleton<PasswordHasher>();

// ===============================
// 5️⃣  CONTROLLERS & JSON CONFIG
// ===============================
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling =
            Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// ===============================
// 6️⃣  JWT AUTHENTICATION
// ===============================
var jwtSettings = configuration.GetSection("Jwt");

// Ensure JWT key exists — throw descriptive error if missing
var jwtKey = jwtSettings["Key"] 
    ?? throw new InvalidOperationException("JWT Key is missing in configuration.");

// Convert safely to bytes
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// ===============================
// 7️⃣  SWAGGER (API Documentation)
// ===============================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Form Builder API",
        Version = "v1",
        Description = "Dynamic Form Builder backend using .NET, SQL, and MongoDB"
    });

    // Enable JWT authentication input in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// ===============================
// 8️⃣  BUILD & RUN APP
// ===============================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // ✅ Automatically generate swagger.yaml in project root
    using (var scope = app.Services.CreateScope())
    {
        var swaggerProvider = scope.ServiceProvider.GetRequiredService<Swashbuckle.AspNetCore.Swagger.ISwaggerProvider>();
        var swaggerDoc = swaggerProvider.GetSwagger("v1");

        var yamlPath = Path.Combine(Directory.GetCurrentDirectory(), "swagger.yaml");

        using (var stream = new FileStream(yamlPath, FileMode.Create))
        using (var writer = new StreamWriter(stream))
        {
            var yamlWriter = new Microsoft.OpenApi.Writers.OpenApiYamlWriter(writer);
            swaggerDoc.SerializeAsV3(yamlWriter);
            writer.Flush();
        }

        Console.WriteLine($"Swagger YAML file generated at: {yamlPath}");
    }

}


app.UseCors("AllowReactApp");

// Seed SQL Database
await DataBaseSeeder.SeedAsync(app.Services);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
