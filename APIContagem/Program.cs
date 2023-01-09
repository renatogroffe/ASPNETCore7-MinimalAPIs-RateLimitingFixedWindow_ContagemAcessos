using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using APIContagem;
using APIContagem.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo
        {
            Title = "APIContagem",
            Description = "Exemplo de implementação de Minimal API para Contagem de acessos e utilizando Rate Limiting (Fixed Window)", 
            Version = "v1",
            Contact = new OpenApiContact()
            {
                Name = "Renato Groffe",
                Url = new Uri("https://github.com/renatogroffe"),
            },
            License = new OpenApiLicense()
            {
                Name = "MIT",
                Url = new Uri("http://opensource.org/licenses/MIT"),
            }
        });
});

const string policyNameRateLimiting = "fixed";
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: policyNameRateLimiting, options =>
    {
        options.PermitLimit = 3;
        options.Window = TimeSpan.FromSeconds(5);
    }));

builder.Services.AddSingleton<Contador>();

builder.Services.AddCors();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "APIContagem v1");
});

app.UseCors(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseRateLimiter();

app.MapGet("/contador", (Contador contador) =>
{
    int valorAtualContador;
    lock (contador)
    {
        contador.Incrementar();
        valorAtualContador = contador.ValorAtual;
    }
    app.Logger.LogInformation($"Contador - Valor atual: {valorAtualContador}");    

    return Results.Ok(new ResultadoContador()
    {
        ValorAtual = contador.ValorAtual,
        Local = contador.Local,
        Kernel = contador.Kernel,
        Framework = contador.Framework,
        Mensagem = app.Configuration["Saudacao"]
    });
})
.RequireRateLimiting(policyNameRateLimiting)
.Produces<ResultadoContador>()
.Produces(StatusCodes.Status503ServiceUnavailable);

app.Run();