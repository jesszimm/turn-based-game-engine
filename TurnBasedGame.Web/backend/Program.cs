using TurnBasedGame.Web.Backend.Stores;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<GameStore>();
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

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("LocalhostFrontend");

app.MapControllers();

app.Run();
