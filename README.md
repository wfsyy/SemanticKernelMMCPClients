# Semantic Kernel MCP 客户端

<div align="center">
  <img src="https://raw.githubusercontent.com/microsoft/semantic-kernel/main/docs/media/sk-logo.png" alt="Semantic Kernel Logo" width="200" />
  <br>
  <em>基于 Microsoft Semantic Kernel 的 MCP 客户端集成</em>
</div>

## 📖 项目简介

本项目是一个基于 Microsoft Semantic Kernel 的应用程序，用于集成和管理多个 MCP (Multi-modal Chat Protocol) 服务客户端。它允许用户通过统一的接口与各种 AI 工具和服务进行交互，包括但不限于 GitHub 工具等。

项目利用 Semantic Kernel 的强大功能，将 MCP 服务的工具映射为 Kernel 函数，使得这些工具可以被 AI 模型（如 GPT-4o）无缝调用。

## ✨ 主要特性

- 🔌 支持多个 MCP 服务的集成和管理
- 🧰 自动将 MCP 工具映射为 Semantic Kernel 函数
- 💬 提供交互式聊天界面，支持流式响应
- ⚙️ 通过配置文件灵活管理 MCP 服务连接
- 🔄 支持动态加载和使用 MCP 服务提供的工具

## 🛠️ 技术栈

- 🔹 C# / .NET
- 🔹 Microsoft Semantic Kernel
- 🔹 OpenAI API (GPT-4o)
- 🔹 MCP (Multi-modal Chat Protocol)

## 📋 前提条件

- .NET 8.0 或更高版本
- 有效的 OpenAI API 密钥
- MCP 服务配置

## 🚀 快速开始

### 安装

1. 克隆此仓库
2. 使用 Visual Studio 或 Visual Studio Code 打开解决方案
3. 恢复 NuGet 包

### 配置

1. 在项目根目录创建 `mcpservices.json` 文件

2. 在 `Program.cs` 中更新 OpenAI API 密钥和端点（如需要）

### 使用方法

运行应用程序后，您将看到一个交互式控制台界面：

1. 输入您的问题或指令
2. 系统将通过 GPT-4o 和已配置的 MCP 工具处理您的请求
3. 查看流式响应结果
4. 输入 "exit" 退出应用程序

## 📝 代码示例

### 初始化 Semantic Kernel 和 MCP 客户端

```csharp
var model = "gpt-4o";
var kernelBuilder = Kernel.CreateBuilder();
var apiKey = "your-openai-api-key";

// 配置OpenAI
kernelBuilder.AddOpenAIChatCompletion(model, new Uri("https://api.openai.com/v1"), apiKey);

// 创建Kernel
var kernel = kernelBuilder.Build();

// 添加MCP服务
var allClients = await McpDotNetExtensions.GetAllMcpClientsAsync().ConfigureAwait(false);
await McpDotNetExtensions.SetKernelToFunction(allClients, kernel);
```

### 使用聊天完成服务

```csharp
var chat = kernel.GetRequiredService<IChatCompletionService>();
var chatHistory = new ChatHistory();
chatHistory.AddUserMessage("您的问题");

await foreach (var item in chat.GetStreamingChatMessageContentsAsync(chatHistory, executionSettings, kernel))
{
    Console.Write(item?.Content);
}
```

## ⚠️ 注意事项

- 请确保安全存储您的 API 密钥，不要将其硬编码在生产代码中
- MCP 服务配置文件应妥善保管，避免泄露敏感信息

## 📄 许可证

[MIT](LICENSE)

## �� 贡献

欢迎提交问题和拉取请求！ 