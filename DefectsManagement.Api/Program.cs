using DefectsManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. НАСТРОЙКА CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; 

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy  =>
        {
            // В режиме разработки разрешаем запросы с любого localhost порта (например, Vite 5173)
            // Это необходимо, так как фронтенд и бэкенд работают на разных портах.
            policy.WithOrigins("http://localhost:5173", "https://localhost:5173") // Конкретный порт Vite
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // Разрешаем передачу куки и заголовков авторизации
        });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- 2. НАСТРОЙКА SWAGGER/JWT ---
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
            new string[] {}
        }
    });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- 3. НАСТРОЙКА АУТЕНТИФИКАЦИИ/АВТОРИЗАЦИИ ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EngineerOnly", policy => policy.RequireRole("Engineer"));
    options.AddPolicy("ManagerOnly", policy => policy.RequireRole("Manager"));
    options.AddPolicy("ObserverOnly", policy => policy.RequireRole("Observer"));
});


var app = builder.Build();

// --- 4. КОНВЕЙЕР MIDDLEWARE ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HttpsRedirection ДОЛЖЕН идти перед UseRouting, UseCors и UseAuthentication
app.UseHttpsRedirection();

// CORS ДОЛЖЕН идти перед UseAuthorization
app.UseCors(MyAllowSpecificOrigins); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();