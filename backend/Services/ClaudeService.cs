using Anthropic;
using Anthropic.Models.Messages;

namespace SmartAgenda.Api.Services;

public class ClaudeService : IClaudeService
{
    private const string DefaultModel = "claude-haiku-5-5";

    private readonly IConfiguration _configuration;
    private readonly Lazy<AnthropicClient> _client;

    public ClaudeService(IConfiguration configuration)
    {
        _configuration = configuration;
        _client = new Lazy<AnthropicClient>(CreateClient);
    }

    public async Task<string> CompleteAsync(
        string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var section = _configuration.GetSection("Anthropic");

        var response = await _client.Value.Messages.Create(
            new MessageCreateParams
            {
                Model = section["Model"] ?? DefaultModel,
                MaxTokens = section.GetValue("MaxTokens", 1024),
                System = systemPrompt,
                // Extracción corta y estructurada: poco razonamiento para cumplir RNF-05 (< 5 s).
                OutputConfig = new OutputConfig { Effort = Effort.Low },
                Messages = [new() { Role = Role.User, Content = userMessage }],
            },
            cancellationToken);

        return string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
    }

    private AnthropicClient CreateClient()
    {
        var section = _configuration.GetSection("Anthropic");
        var apiKey = section["ApiKey"]
            ?? throw new InvalidOperationException("Falta configurar Anthropic:ApiKey (ver user-secrets).");

        return new AnthropicClient
        {
            ApiKey = apiKey,
            Timeout = TimeSpan.FromSeconds(section.GetValue("TimeoutSeconds", 4)),
            MaxRetries = section.GetValue("MaxRetries", 0),
        };
    }
}
