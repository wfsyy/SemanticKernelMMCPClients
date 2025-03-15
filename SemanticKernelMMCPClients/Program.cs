// See https://aka.ms/new-console-template for more information
#pragma warning disable SKEXP0001 // API可能会在未来版本中更改
#pragma warning disable SKEXP0010 // API可能会在未来版本中更改
#pragma warning disable SKEXP0110 // API可能会在未来版本中更改
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelMMCPClients;

Console.WriteLine("Hello, World!");


var model = "";
var kernelBuilder = Kernel.CreateBuilder();
var apiKey = "";
// 配置OpenAI
kernelBuilder.AddOpenAIChatCompletion(model, new Uri("http://api.token-ai.cn/v1"), apiKey);
// 创建Kernel
var kernel = kernelBuilder.Build();
# region 添加MCP服务
//添加MCP服务
var allClients = await McpDotNetExtensions.GetAllMcpClientsAsync().ConfigureAwait(false);
await McpDotNetExtensions.SetKernelToFunction(allClients, kernel);
#endregion

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};
var chat = kernel.GetRequiredService<IChatCompletionService>();

while (true)
{
    Console.WriteLine("请输入您的问题：");
    var str = Console.ReadLine();

    if (str == "exit")
    {
        break;
    }

    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage(str);
    await foreach (var item in  chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
    {
        Console.Write(item?.Content);
    }

    Console.WriteLine();
}














#pragma warning restore SKEXP0110
#pragma warning restore SKEXP0010
#pragma warning restore SKEXP0001