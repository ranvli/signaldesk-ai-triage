using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using SignalDesk.Endpoints;
using SignalDesk.Infrastructure.Data;
using SignalDesk.Infrastructure.Repositories;
using SignalDesk.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SignalDesk API", Version = "v1" }));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Database
builder.Services.AddDbContext<SignalDeskDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=signaldesk.db"));

builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();

// AI service
builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.Section));
builder.Services.AddHttpClient<IFeedbackAiService, OllamaFeedbackAiService>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(opts.BaseUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SignalDeskDbContext>().Database.EnsureCreated();

app.MapFeedbackEndpoints();

app.Run();

public partial class Program { }

