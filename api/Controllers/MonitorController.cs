using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

// Minimal ChatMessage and ChatRole types for MCP API compatibility
// public enum ChatRole { User, Assistant, System }

// public class ChatMessage
// {
//     public ChatRole Role { get; set; }
//     public string? Content { get; set; }

//     public ChatMessage() { }
//     public ChatMessage(ChatRole role, string? content)
//     {
//         Role = role;
//         Content = content;
//     }
// }

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitorController : ControllerBase
    {
        private readonly Kernel _kernel;

        public MonitorController(Kernel kernel)
        {
            _kernel = kernel;
        }

        [HttpPost("chat/stream")]
        public async Task ChatStream([FromBody] ChatRequest request)
        {
            Response.ContentType = "text/event-stream";

            // 1. Create the MCP client
            await using var mcpClient = await McpClientFactory.CreateAsync(
                new StdioClientTransport(new()
                {
                    Command = "npx",
                    Arguments = ["-y", "@azure/mcp@latest", "server", "start"],
                    Name = "Azure MCP",
                }));

            // 2. Get all available tools from the MCP server
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
            _kernel.Plugins.AddFromFunctions("Tools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

            // 3. Enable automatic function calling
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };

            // 4. Prepare chat message history
            var messages = new ChatHistory();
            if (request.History != null)
                messages.AddRange(request.History);
            if (!string.IsNullOrWhiteSpace(request.Message))
                messages.AddUserMessage(request.Message);

            // 5. Define the agent
            var agent = new ChatCompletionAgent
            {
                Instructions = "Answer questions about the weather.",
                Name = "WeatherAgent",
                Kernel = _kernel,
                Arguments = new KernelArguments(executionSettings),
            };

            // 6. Stream the response as it is generated
            //var messageContentList = messages.
            await foreach (var update in agent.InvokeStreamingAsync(messages))
            {
                var chunk = update?.Message?.Content?.ToString();
                if (!string.IsNullOrEmpty(chunk))
                {
                    await Response.WriteAsync($"data: {chunk}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public List<ChatMessageContent>? History { get; set; }
        // Optionally, add properties for client config if needed
        public object? Client { get; set; }
    }

    public class ChatResponse
    {
        public IEnumerable<object>? AvailableTools { get; set; }
        public IEnumerable<ChatMessageContent>? Messages { get; set; }
        public string? Info { get; set; }
    }
}
