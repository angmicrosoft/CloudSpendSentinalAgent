
using Microsoft.SemanticKernel;
using Api.Controllers;
using Microsoft.OpenApi.Models;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add Semantic Kernel to DI (use Kernel instead of IKernel)
var configuration = builder.Configuration;
var kernelBuilder = Kernel.CreateBuilder();
var s_azureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions {  TenantId = configuration["AzureAD:TenantId"] });

var azureOpenAIServiceEndpoint = configuration.GetSection("AzureOpenAI:Endpoint").Value;
ArgumentNullException.ThrowIfNull(azureOpenAIServiceEndpoint, nameof(azureOpenAIServiceEndpoint));

var gptDeploymentName = configuration.GetSection("AzureOpenAI:DeploymentModelName").Value;
ArgumentNullException.ThrowIfNull(gptDeploymentName, nameof(gptDeploymentName));

var embeddingModelName = configuration.GetSection("AzureOpenAI:TextEmbeddingModelName").Value;
ArgumentNullException.ThrowIfNull(embeddingModelName, nameof(embeddingModelName));
// var azureOpenAiKey = configurationManager["AzureOpenAIKey"];
// ArgumentNullException.ThrowIfNull(azureOpenAiKey, nameof(azureOpenAiKey));

kernelBuilder.AddAzureOpenAIEmbeddingGenerator(deploymentName: embeddingModelName,
                                    endpoint: azureOpenAIServiceEndpoint,
                                    credential: s_azureCredential);
kernelBuilder.AddAzureOpenAIChatCompletion(endpoint: azureOpenAIServiceEndpoint,
                                    deploymentName: gptDeploymentName,
                                    serviceId: gptDeploymentName,
                                    credentials: s_azureCredential);

builder.Services.AddSingleton<Kernel>(kernelBuilder.Build());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Monitor API", Version = "v1" });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.UseCors();
app.Run();
