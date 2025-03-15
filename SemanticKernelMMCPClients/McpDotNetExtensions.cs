using McpDotNet.Client;
using McpDotNet.Configuration;
using McpDotNet.Protocol.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticKernelMMCPClients
{
    /// <summary>
    /// MCP服务配置文件模型
    /// </summary>
    public class McpServicesConfig
    {
        [JsonPropertyName("McpServices")]
        public List<McpServerConfig> McpServices { get; set; } = new List<McpServerConfig>();
    }

    public static class McpDotNetExtensions
    {
        private const string DEFAULT_CONFIG_PATH = "mcpservices.json";
        private const string CONFIG_PATH_ENV_VAR = "MCP_CONFIG_PATH";

        /// <summary>
        /// 获取指定ID的MCP客户端
        /// </summary>
        /// <param name="id">MCP服务ID</param>
        /// <param name="configPath">配置文件路径，如果为null则使用默认路径</param>
        /// <param name="loggerFactory">日志工厂，如果为null则使用NullLoggerFactory</param>
        /// <returns>MCP客户端实例</returns>
        public static async Task<IMcpClient> GetMcpClientByIdAsync(string id, string? configPath = null, ILoggerFactory? loggerFactory = null)
        {
            var configs = await LoadMcpConfigsAsync(configPath);
            var config = configs.FirstOrDefault(c => c.Id == id);

            if (config == null)
            {
                throw new ArgumentException($"未找到ID为'{id}'的MCP服务配置");
            }

            var options = new McpClientOptions
            {
                ClientInfo = new() { Name = config.Name, Version = "1.0.0" }
            };

            var factory = new McpClientFactory(
                [config],
                options,
                loggerFactory ?? NullLoggerFactory.Instance
            );

            return await factory.GetClientAsync(config.Id).ConfigureAwait(false);
        }

        /// <summary>
        /// 获取GitHub工具客户端
        /// </summary>
        /// <param name="configPath">配置文件路径，如果为null则使用默认路径</param>
        /// <param name="loggerFactory">日志工厂，如果为null则使用NullLoggerFactory</param>
        /// <returns>GitHub工具客户端实例</returns>
        public static async Task<IMcpClient> GetGitHubToolsAsync(string? configPath = null, ILoggerFactory? loggerFactory = null)
        {
            return await GetMcpClientByIdAsync("github", configPath, loggerFactory);
        }

        /// <summary>
        /// 获取所有配置的MCP客户端
        /// </summary>
        /// <param name="configPath">配置文件路径，如果为null则使用默认路径</param>
        /// <param name="loggerFactory">日志工厂，如果为null则使用NullLoggerFactory</param>
        /// <returns>MCP客户端字典，键为服务ID</returns>
        public static async Task<Dictionary<string, IMcpClient>> GetAllMcpClientsAsync(string? configPath = null, ILoggerFactory? loggerFactory = null)
        {
            var configs = await LoadMcpConfigsAsync(configPath);
            var result = new Dictionary<string, IMcpClient>();
            var options = new McpClientOptions
            {
                ClientInfo = new() { Name = "MCP Client", Version = "1.0.0" }
            };

            var factory = new McpClientFactory(
                configs,
                options,
                loggerFactory ?? NullLoggerFactory.Instance
            );

            foreach (var config in configs)
            {
                try
                {
                    var client = await factory.GetClientAsync(config.Id).ConfigureAwait(false);
                    result.Add(config.Id, client);
                }
                catch (Exception ex)
                {
                    // 记录错误但继续处理其他客户端
                    var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(typeof(McpDotNetExtensions));
                    logger.LogError(ex, "无法创建ID为'{Id}'的MCP客户端", config.Id);
                }
            }

            return result;
        }

        /// <summary>
        /// 将MCP客户端的工具映射为Semantic Kernel函数
        /// </summary>
        /// <param name="mcpClient">MCP客户端</param>
        /// <returns>Kernel函数集合</returns>
        public static async Task<IEnumerable<KernelFunction>> MapToFunctionsAsync(this IMcpClient mcpClient)
        {
            var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
            return tools.Tools.Select(t => t.ToKernelFunction(mcpClient)).ToList();
        }
        /// <summary>
        /// 给Kernel添加插件
        /// </summary>
        /// <param name="allClients"></param>
        /// <param name="Kernel"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static async Task SetKernelToFunction(Dictionary<string, IMcpClient> allClients, Kernel Kernel)
        {
            // 遍历所有MCP客户端
            foreach (var clientEntry in allClients)
            {
                string serviceId = clientEntry.Key;
                IMcpClient mcpClient = clientEntry.Value;

                Console.WriteLine($"已连接到MCP服务: {serviceId}");
                try
                {
                    // 获取该服务上可用的工具列表
                    var tools = await mcpClient.ListToolsAsync().ConfigureAwait(false);
                    Console.WriteLine($"服务 {serviceId} 提供了 {tools.Tools.Count} 个工具:");

                    foreach (var tool in tools.Tools)
                    {
                        Console.WriteLine($"  - {tool.Name}: {tool.Description}");
                    }

                    // 将该服务的工具添加为Kernel函数
                    var functions = await mcpClient.MapToFunctionsAsync().ConfigureAwait(false);
                    Kernel.Plugins.AddFromFunctions(serviceId, functions);

                    Console.WriteLine($"已将服务 {serviceId} 的 {functions.Count()} 个函数添加到Kernel中");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"处理服务 {serviceId} 时出错: {ex.Message}");
                }
            }
        }
        #region private methods
        /// <summary>
        /// 加载MCP配置
        /// </summary>
        /// <param name="configPath">配置文件路径，如果为null则使用默认路径</param>
        /// <returns>MCP服务配置列表</returns>
        private static async Task<List<McpServerConfig>> LoadMcpConfigsAsync(string? configPath = null)
        {
            // 确定配置文件路径
            string finalPath = configPath ?? Environment.GetEnvironmentVariable(CONFIG_PATH_ENV_VAR) ?? DEFAULT_CONFIG_PATH;

            try
            {
                // 读取配置文件
                if (!File.Exists(finalPath))
                {
                    throw new FileNotFoundException($"MCP配置文件不存在: {finalPath}");
                }

                string jsonContent = await File.ReadAllTextAsync(finalPath);
                var config = JsonSerializer.Deserialize<McpServicesConfig>(jsonContent);

                return config?.McpServices ?? new List<McpServerConfig>();
            }
            catch (Exception ex) when (ex is not FileNotFoundException)
            {
                throw new InvalidOperationException($"无法解析MCP配置文件: {finalPath}", ex);
            }
        }

        private static KernelFunction ToKernelFunction(this Tool tool, IMcpClient mcpClient)
        {
            async Task<string> InvokeToolAsync(Kernel kernel, KernelFunction function, KernelArguments arguments, CancellationToken cancellationToken)
            {
                try
                {
                    // 转换参数为mcpdotnet期望的字典格式
                    Dictionary<string, object> mcpArguments = [];
                    foreach (var arg in arguments)
                    {
                        if (arg.Value is not null)
                        {
                            mcpArguments[arg.Key] = function.ToArgumentValue(arg.Key, arg.Value);
                        }
                    }

                    // 通过mcpdotnet调用工具
                    var result = await mcpClient.CallToolAsync(
                        tool.Name,
                        mcpArguments,
                        cancellationToken: cancellationToken
                    ).ConfigureAwait(false);

                    // 从结果中提取文本内容
                    return string.Join("\n", result.Content
                        .Where(c => c.Type == "text")
                        .Select(c => c.Text));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"调用工具'{tool.Name}'时出错: {ex.Message}");

                    // 重新抛出异常，让kernel处理
                    throw;
                }
            }

            return KernelFunctionFactory.CreateFromMethod(
                method: InvokeToolAsync,
                functionName: tool.Name,
                description: tool.Description,
                parameters: tool.ToParameters(),
                returnParameter: ToReturnParameter()
            );
        }

        private static object ToArgumentValue(this KernelFunction function, string name, object value)
        {
            var parameter = function.Metadata.Parameters.FirstOrDefault(p => p.Name == name);
            return parameter?.ParameterType switch
            {
                Type t when Nullable.GetUnderlyingType(t) == typeof(int) => Convert.ToInt32(value),
                Type t when Nullable.GetUnderlyingType(t) == typeof(double) => Convert.ToDouble(value),
                Type t when Nullable.GetUnderlyingType(t) == typeof(bool) => Convert.ToBoolean(value),
                Type t when t == typeof(List<string>) => (value as IEnumerable<object>)?.ToList(),
                Type t when t == typeof(Dictionary<string, object>) => (value as Dictionary<string, object>)?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                _ => value,
            } ?? value;
        }

        private static List<KernelParameterMetadata>? ToParameters(this Tool tool)
        {
            var inputSchema = tool.InputSchema;
            var properties = inputSchema?.Properties;
            if (properties == null)
            {
                return null;
            }

            HashSet<string> requiredProperties = new(inputSchema!.Required ?? []);
            return properties.Select(kvp =>
                new KernelParameterMetadata(kvp.Key)
                {
                    Description = kvp.Value.Description,
                    ParameterType = ConvertParameterDataType(kvp.Value, requiredProperties.Contains(kvp.Key)),
                    IsRequired = requiredProperties.Contains(kvp.Key)
                }).ToList();
        }

        private static KernelReturnParameterMetadata? ToReturnParameter()
        {
            return new KernelReturnParameterMetadata()
            {
                ParameterType = typeof(string),
            };
        }

        private static Type ConvertParameterDataType(JsonSchemaProperty property, bool required)
        {
            var type = property.Type switch
            {
                "string" => typeof(string),
                "integer" => typeof(int),
                "number" => typeof(double),
                "boolean" => typeof(bool),
                "array" => typeof(List<string>),
                "object" => typeof(Dictionary<string, object>),
                _ => typeof(object)
            };

            return !required && type.IsValueType ? typeof(Nullable<>).MakeGenericType(type) : type;
        }


        #endregion
    }
}
