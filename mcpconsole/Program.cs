using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using Azure;
//using ModelContextProtocol.Protocol.Transport;

// Create an IChatClient
IChatClient client =
    new ChatClientBuilder(
        new AzureOpenAIClient(new Uri("https://tguru-dev-openai-0.openai.azure.com/"),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions {  TenantId = "16b3c013-d300-468d-ac64-7eda0820b6d3" }))
        .GetChatClient("gpt-4o").AsIChatClient())
    .UseFunctionInvocation()
    .Build();

// Create the MCP client
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = "npx",
        Arguments = ["-y", "@azure/mcp@latest", "server", "start"],
        Name = "Azure MCP",
    }));

// Get all available tools from the MCP server
Console.WriteLine("Available tools:");
var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"{tool}");
}
Console.WriteLine();

// Conversational loop that can utilize the tools
List<ChatMessage> messages = [];
while (true)
{
    Console.Write("Prompt: ");
    messages.Add(new(ChatRole.User, Console.ReadLine()));

    List<ChatResponseUpdate> updates = [];
    await foreach (var update in client
        .GetStreamingResponseAsync(messages, new() { Tools = [.. tools] }))
    {
        Console.Write(update);
        updates.Add(update);
    }
    Console.WriteLine();

    messages.AddMessages(updates);
}