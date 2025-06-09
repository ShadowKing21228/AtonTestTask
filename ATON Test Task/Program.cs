using ATON_Test_Task.Controllers;
using ATON_Test_Task.Data;
using ATON_Test_Task.Repositories;
using ATON_Test_Task.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<DbConnectionFactory>(options =>
    options.UseSqlite("Data Source=app.db"));
await UsersRepository.InitializeDatabaseAsync();

builder.Services.AddSwaggerGen(options => {
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ATON Test Task",
        Version = "v1",
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введите JWT токен в поле ниже. Формат: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement { 
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = true;
        options.SaveToken = true;
        options.TokenValidationParameters = JwtHandler.JwtParameters;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    if (!dbContext.Users.Any())
    {
        dbContext.Users.Add(new User
        {
            Login = "admin",
            Password = "admin",
            Name = "Admin",
            Gender = 1,
            Birthday = new DateTime(1999, 1, 20),
            Admin = true,
            CreatedBy = "System"
        });

        dbContext.SaveChanges();
    }
    var db = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    if (!db.Database.CanConnect()) db.Database.Migrate();
}

app.MapOpenApi("/swagger/v1/swagger.json");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ATON Test Task");
    });

}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();