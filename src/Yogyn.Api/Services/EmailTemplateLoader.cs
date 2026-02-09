using System.Web;

namespace Yogyn.Api.Services;

public class EmailTemplateLoader
{
    private readonly Dictionary<string, string> _templates = new();
    private readonly ILogger<EmailTemplateLoader> _logger;

    public EmailTemplateLoader(IWebHostEnvironment env, ILogger<EmailTemplateLoader> logger)
    {
        _logger = logger;
        LoadTemplates(env.ContentRootPath);
    }

    private void LoadTemplates(string contentRootPath)
    {
        var templatePath = Path.Combine(contentRootPath, "EmailTemplates");
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Email templates directory not found: {templatePath}");
        }

        var templateFiles = new[]
        {
            "confirmation",
            "pending",
            "approved",
            "rejected",
            "cancelled"
        };

        foreach (var templateName in templateFiles)
        {
            var filePath = Path.Combine(templatePath, $"{templateName}.html");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Template file not found: {filePath}");
            }

            _templates[templateName] = File.ReadAllText(filePath);
            _logger.LogInformation("Loaded email template: {TemplateName}", templateName);
        }

        _logger.LogInformation("All email templates loaded successfully");
    }

    public string RenderTemplate(string templateName, Dictionary<string, string> placeholders, bool htmlEncode = true)
    {
        if (!_templates.TryGetValue(templateName, out var template))
        {
            throw new ArgumentException($"Template '{templateName}' not found", nameof(templateName));
        }

        foreach (var placeholder in placeholders)
        {
            var value = htmlEncode && !placeholder.Key.EndsWith("Section") && !placeholder.Key.EndsWith("Message")
                ? HttpUtility.HtmlEncode(placeholder.Value)
                : placeholder.Value;
            
            template = template.Replace($"{{{{{placeholder.Key}}}}}", value);
        }

        ValidateTemplate(template, templateName);
        
        return template;
    }

    private void ValidateTemplate(string template, string templateName)
    {
        if (template.Contains("{{") && template.Contains("}}"))
        {
            var unreplacedPlaceholder = ExtractFirstPlaceholder(template);
            _logger.LogWarning(
                "Template '{TemplateName}' contains unreplaced placeholder: {Placeholder}", 
                templateName, 
                unreplacedPlaceholder
            );
        }
    }

    private string ExtractFirstPlaceholder(string template)
    {
        var startIndex = template.IndexOf("{{");
        var endIndex = template.IndexOf("}}", startIndex);
        
        if (startIndex >= 0 && endIndex > startIndex)
        {
            return template.Substring(startIndex, endIndex - startIndex + 2);
        }
        
        return "unknown";
    }
}
