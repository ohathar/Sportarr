using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sportarr.Api.Data;
using Sportarr.Api.Models;

namespace Sportarr.Api.Services;

/// <summary>
/// Service for sending notifications through various providers (Discord, Telegram, Pushover, etc.)
/// </summary>
public class NotificationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;
    private readonly HttpClient _httpClient;

    public NotificationService(
        IServiceProvider serviceProvider,
        ILogger<NotificationService> logger,
        HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Send a notification through all enabled notification providers that match the trigger
    /// </summary>
    public async Task SendNotificationAsync(NotificationTrigger trigger, string title, string message, Dictionary<string, object>? metadata = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SportarrDbContext>();

        var notifications = await db.Notifications.Where(n => n.Enabled).ToListAsync();

        foreach (var notification in notifications)
        {
            try
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(notification.ConfigJson) ?? new();

                // Check if this notification is configured for the trigger
                if (!ShouldSendForTrigger(config, trigger))
                    continue;

                var success = notification.Implementation switch
                {
                    "Discord" => await SendDiscordAsync(config, title, message),
                    "Telegram" => await SendTelegramAsync(config, title, message),
                    "Pushover" => await SendPushoverAsync(config, title, message),
                    "Slack" => await SendSlackAsync(config, title, message),
                    "Webhook" => await SendWebhookAsync(config, title, message, trigger, metadata),
                    "Email" => await SendEmailAsync(config, title, message),
                    _ => false
                };

                if (success)
                {
                    _logger.LogDebug("Sent {Trigger} notification via {Implementation}: {Title}", trigger, notification.Implementation, title);
                }
                else
                {
                    _logger.LogWarning("Failed to send {Trigger} notification via {Implementation}: {Title}", trigger, notification.Implementation, title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification via {Implementation}", notification.Implementation);
            }
        }
    }

    /// <summary>
    /// Test a notification configuration
    /// </summary>
    public async Task<(bool Success, string Message)> TestNotificationAsync(Notification notification)
    {
        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(notification.ConfigJson) ?? new();

            var success = notification.Implementation switch
            {
                "Discord" => await SendDiscordAsync(config, "Test Notification", "This is a test notification from Sportarr."),
                "Telegram" => await SendTelegramAsync(config, "Test Notification", "This is a test notification from Sportarr."),
                "Pushover" => await SendPushoverAsync(config, "Test Notification", "This is a test notification from Sportarr."),
                "Slack" => await SendSlackAsync(config, "Test Notification", "This is a test notification from Sportarr."),
                "Webhook" => await SendWebhookAsync(config, "Test Notification", "This is a test notification from Sportarr.", NotificationTrigger.Test, null),
                "Email" => await SendEmailAsync(config, "Test Notification", "This is a test notification from Sportarr."),
                _ => false
            };

            return success
                ? (true, "Notification sent successfully!")
                : (false, "Failed to send notification. Check your configuration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing notification");
            return (false, $"Error: {ex.Message}");
        }
    }

    private bool ShouldSendForTrigger(Dictionary<string, JsonElement> config, NotificationTrigger trigger)
    {
        var fieldName = trigger switch
        {
            NotificationTrigger.OnGrab => "onGrab",
            NotificationTrigger.OnDownload => "onDownload",
            NotificationTrigger.OnUpgrade => "onUpgrade",
            NotificationTrigger.OnRename => "onRename",
            NotificationTrigger.OnHealthIssue => "onHealthIssue",
            NotificationTrigger.OnApplicationUpdate => "onApplicationUpdate",
            NotificationTrigger.Test => null, // Always send test notifications
            _ => null
        };

        if (fieldName == null) return true;

        return config.TryGetValue(fieldName, out var value) && value.ValueKind == JsonValueKind.True;
    }

    private string GetConfigString(Dictionary<string, JsonElement> config, string key, string defaultValue = "")
    {
        return config.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? defaultValue
            : defaultValue;
    }

    private int GetConfigInt(Dictionary<string, JsonElement> config, string key, int defaultValue = 0)
    {
        if (!config.TryGetValue(key, out var value)) return defaultValue;
        return value.ValueKind == JsonValueKind.Number ? value.GetInt32() : defaultValue;
    }

    #region Discord

    private async Task<bool> SendDiscordAsync(Dictionary<string, JsonElement> config, string title, string message)
    {
        var webhook = GetConfigString(config, "webhook");
        var username = GetConfigString(config, "username", "Sportarr");

        if (string.IsNullOrEmpty(webhook))
        {
            _logger.LogWarning("Discord webhook URL not configured");
            return false;
        }

        var payload = new
        {
            username,
            embeds = new[]
            {
                new
                {
                    title,
                    description = message,
                    color = 0xDC2626 // Red color matching Sportarr theme
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(webhook, content);

        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Telegram

    private async Task<bool> SendTelegramAsync(Dictionary<string, JsonElement> config, string title, string message)
    {
        var token = GetConfigString(config, "token");
        var chatId = GetConfigString(config, "chatId");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            _logger.LogWarning("Telegram bot token or chat ID not configured");
            return false;
        }

        var url = $"https://api.telegram.org/bot{token}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text = $"*{EscapeMarkdown(title)}*\n\n{EscapeMarkdown(message)}",
            parse_mode = "Markdown"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);

        return response.IsSuccessStatusCode;
    }

    private static string EscapeMarkdown(string text)
    {
        // Escape special Markdown characters
        return text
            .Replace("_", "\\_")
            .Replace("*", "\\*")
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace("~", "\\~")
            .Replace("`", "\\`")
            .Replace(">", "\\>")
            .Replace("#", "\\#")
            .Replace("+", "\\+")
            .Replace("-", "\\-")
            .Replace("=", "\\=")
            .Replace("|", "\\|")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace(".", "\\.")
            .Replace("!", "\\!");
    }

    #endregion

    #region Pushover

    private async Task<bool> SendPushoverAsync(Dictionary<string, JsonElement> config, string title, string message)
    {
        var userKey = GetConfigString(config, "userKey");
        var apiToken = GetConfigString(config, "apiToken");
        var devices = GetConfigString(config, "devices");
        var priority = GetConfigInt(config, "priority", 0);
        var sound = GetConfigString(config, "sound", "pushover");
        var retry = GetConfigInt(config, "retry", 60);
        var expire = GetConfigInt(config, "expire", 3600);

        if (string.IsNullOrEmpty(userKey) || string.IsNullOrEmpty(apiToken))
        {
            _logger.LogWarning("Pushover user key or API token not configured");
            return false;
        }

        var formData = new List<KeyValuePair<string, string>>
        {
            new("token", apiToken),
            new("user", userKey),
            new("title", title),
            new("message", message),
            new("priority", priority.ToString()),
            new("sound", sound)
        };

        // Add device targeting if specified
        if (!string.IsNullOrEmpty(devices))
        {
            formData.Add(new("device", devices));
        }

        // Emergency priority requires retry and expire parameters
        if (priority == 2)
        {
            formData.Add(new("retry", Math.Max(30, retry).ToString()));
            formData.Add(new("expire", Math.Min(10800, expire).ToString()));
        }

        var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync("https://api.pushover.net/1/messages.json", content);

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Pushover API returned {StatusCode}: {Response}", response.StatusCode, responseBody);
        }

        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Slack

    private async Task<bool> SendSlackAsync(Dictionary<string, JsonElement> config, string title, string message)
    {
        var webhook = GetConfigString(config, "webhook");
        var username = GetConfigString(config, "username", "Sportarr");
        var channel = GetConfigString(config, "channel");

        if (string.IsNullOrEmpty(webhook))
        {
            _logger.LogWarning("Slack webhook URL not configured");
            return false;
        }

        var payload = new Dictionary<string, object>
        {
            ["username"] = username,
            ["attachments"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["fallback"] = $"{title}: {message}",
                    ["color"] = "#DC2626",
                    ["title"] = title,
                    ["text"] = message
                }
            }
        };

        if (!string.IsNullOrEmpty(channel))
        {
            payload["channel"] = channel;
        }

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(webhook, content);

        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Webhook

    private async Task<bool> SendWebhookAsync(Dictionary<string, JsonElement> config, string title, string message, NotificationTrigger trigger, Dictionary<string, object>? metadata)
    {
        var webhook = GetConfigString(config, "webhook");

        if (string.IsNullOrEmpty(webhook))
        {
            _logger.LogWarning("Webhook URL not configured");
            return false;
        }

        var payload = new Dictionary<string, object>
        {
            ["eventType"] = trigger.ToString(),
            ["title"] = title,
            ["message"] = message,
            ["applicationUrl"] = "", // Could be filled with app URL if available
            ["instanceName"] = "Sportarr"
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                payload[kvp.Key] = kvp.Value;
            }
        }

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(webhook, content);

        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Email

    private async Task<bool> SendEmailAsync(Dictionary<string, JsonElement> config, string title, string message)
    {
        var server = GetConfigString(config, "server");
        var port = GetConfigInt(config, "port", 587);
        var username = GetConfigString(config, "username");
        var password = GetConfigString(config, "password");
        var from = GetConfigString(config, "from");
        var to = GetConfigString(config, "to");
        var useSsl = config.TryGetValue("useSsl", out var sslValue) && sslValue.ValueKind == JsonValueKind.True;

        if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            _logger.LogWarning("Email server, from, or to address not configured");
            return false;
        }

        try
        {
            using var client = new System.Net.Mail.SmtpClient(server, port)
            {
                EnableSsl = useSsl
            };

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new System.Net.NetworkCredential(username, password);
            }

            var mailMessage = new System.Net.Mail.MailMessage(from, to, title, message)
            {
                IsBodyHtml = false
            };

            await client.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email notification");
            return false;
        }
    }

    #endregion
}

/// <summary>
/// Types of notification triggers
/// </summary>
public enum NotificationTrigger
{
    OnGrab,
    OnDownload,
    OnUpgrade,
    OnRename,
    OnHealthIssue,
    OnApplicationUpdate,
    Test
}
