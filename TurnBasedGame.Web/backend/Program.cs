var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<TurnBasedGame.Web.Backend.Stores.GameStore>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalhostFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors("LocalhostFrontend");

app.MapControllers();

app.Run();
