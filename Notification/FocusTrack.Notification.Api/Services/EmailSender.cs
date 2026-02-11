using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FocusTrack.Notification.Api.Services;

//Sends email for offline users when a session is shared
public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    private readonly IConfiguration _configuration;

    public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendSessionSharedNotificationAsync(string recipientUserId, Guid sessionId, string ownerUserId, CancellationToken ct = default)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        if (string.IsNullOrEmpty(smtpHost))
        {
            _logger.LogInformation(
                "Email (stub): session shared with user {RecipientUserId}, SessionId={SessionId}, Owner={OwnerUserId}. Set Email:SmtpHost and Email:UserIdToEmail for real email.",
                recipientUserId, sessionId, ownerUserId);
            return;
        }

        var recipientEmail = _configuration["Email:UserIdToEmail:" + recipientUserId]
            ?? _configuration.GetSection("Email:UserIdToEmail")[recipientUserId];
        if (string.IsNullOrEmpty(recipientEmail))
        {
            _logger.LogWarning(
                "No email mapping for user {RecipientUserId}. Add Email:UserIdToEmail:{RecipientUserId} in config. Skipping send.",
                recipientUserId, recipientUserId);
            return;
        }

        var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@focustrack.local";
        var fromName = _configuration["Email:FromName"] ?? "FocusTrack";
        var port = _configuration.GetValue<int>("Email:SmtpPort", 587);
        var useSsl = _configuration.GetValue<bool>("Email:SmtpUseSsl", false);
        var userName = _configuration["Email:SmtpUserName"];
        var password = _configuration["Email:SmtpPassword"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = "A focus session was shared with you";
        message.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = $@"
<h2>Session shared with you</h2>
<p>A focus session has been shared with you.</p>
<ul>
  <li><strong>Session ID:</strong> {sessionId}</li>
  <li><strong>Shared by:</strong> {ownerUserId}</li>
</ul>
<p>Sign in to FocusTrack to view it.</p>
"
        };

        try
        {
            using var client = new SmtpClient();
            var secureSocketOptions = port == 465 ? SecureSocketOptions.SslOnConnect
                : useSsl ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(smtpHost, port, secureSocketOptions, ct);
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                await client.AuthenticateAsync(userName, password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
            _logger.LogInformation(
                "Email sent: session shared notification to {RecipientEmail} (userId={RecipientUserId}), SessionId={SessionId}",
                recipientEmail, recipientUserId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send session-shared email to {RecipientEmail} (userId={RecipientUserId}), SessionId={SessionId}",
                recipientEmail, recipientUserId, sessionId);
            throw;
        }
    }
}
