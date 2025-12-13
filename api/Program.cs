var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("https://curly-eureka-77r64vg95v63rj4v-4200.app.github.dev")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline
// Use forwarded headers from reverse proxy FIRST (for HTTPS detection)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto | 
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
});

// Enable CORS early in the pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // Don't require HTTPS redirect in development/Docker
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow })
    .WithName("Health Check")
    .WithOpenApi();

app.Run();
