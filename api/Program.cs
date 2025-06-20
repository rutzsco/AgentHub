var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Status endpoint
app.MapGet("/status", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }))
    .WithName("GetStatus")
    .WithOpenApi();

app.Run();
