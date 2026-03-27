using TurnBasedGame.Web.Backend.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(TurnBasedGame.Web.Backend.Controllers.GameController).Assembly);
builder.Services.AddSingleton<GameStore>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProdCors", policy =>
    {
        policy.WithOrigins("https://turn-based-game-engine.onrender.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ProdCors");

app.MapGet("/", () => "API is running");

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");
