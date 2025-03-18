using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WhatsAppClone.DTOs;
using WhatsAppClone.Models;
using WhatsAppClone.Validators;

var builder = WebApplication.CreateBuilder(args);
// Configuration'dan baðlantý cümlesini al
var connectionString = builder.Configuration.GetConnectionString("EkikWhatsappContext");

// DbContext'i Dependency Injection'a ekle
builder.Services.AddDbContext<EkikWhatsappContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddAuthentication().AddJwtBearer(cfr =>
{
    cfr.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = "Issuer",
        ValidAudience = "Audience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret key my secret key my secret key"))
    };
});

// Add services to the container.
builder.Services.AddTransient<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddTransient<IValidator<CreatePersonelDto>, PersonelValidator>();
builder.Services.AddTransient<IValidator<LoginDto>, LoginValidator>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});
builder.Services.AddSwaggerGen(setup =>
{
    var jwtSecuritySheme = new OpenApiSecurityScheme
    {
        BearerFormat = "JWT",
        Name = "JWT Authentication",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        Description = "Put **_ONLY_** yourt JWT Bearer token on textbox below!",

        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    setup.AddSecurityDefinition(jwtSecuritySheme.Reference.Id, jwtSecuritySheme);

    setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { jwtSecuritySheme, Array.Empty<string>() }
                });
});
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    //c.SwaggerEndpoint("/whatsapp_clone/swagger/v1/swagger.json", "My API V1");
    c.InjectStylesheet("../swagger-ui/custom.css");
    c.InjectJavascript("../swagger-ui/custom.js");
});
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();




app.Run();
