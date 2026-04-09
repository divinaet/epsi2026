using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// ── Swagger / OpenAPI ──────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "Star Wars API",
        Version = "v1",
        Description = "Une API REST pour gérer les personnages Star Wars.",
        Contact = new OpenApiContact
        {
            Name  = "Padawan Dev",
            Email = "padawan@jedi-temple.sw"
        }
    });
});
// ───────────────────────────────────────────────────────────


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Active Swagger UI uniquement en développement
    app.UseSwagger();      // Génère /swagger/v1/swagger.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Star Wars API v1");
        options.RoutePrefix = string.Empty; // Swagger UI accessible à la racine /
    });

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
