using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DesiCorner.AuthServer.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["Email:From"]));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = body
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["Email:SmtpServer"],
                int.Parse(_config["Email:SmtpPort"]!),
                SecureSocketOptions.StartTls,
                ct);

            await smtp.AuthenticateAsync(
                _config["Email:Username"],
                _config["Email:Password"],
                ct);

            await smtp.SendAsync(email, ct);
            await smtp.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            return false;
        }
    }

    public async Task<bool> SendOtpEmailAsync(string to, string otp, string purpose, CancellationToken ct = default)
    {
        var subject = $"DesiCorner - Your OTP Code";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #FF6B35 0%, #F7931E 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .otp-code {{ background: #fff; border: 2px dashed #FF6B35; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; color: #FF6B35; letter-spacing: 5px; margin: 20px 0; border-radius: 5px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍛 DesiCorner</h1>
            <p>Authentic Indian Cuisine</p>
        </div>
        <div class='content'>
            <h2>Your Verification Code</h2>
            <p>Hello!</p>
            <p>You requested a verification code for <strong>{purpose}</strong>. Please use the code below:</p>
            
            <div class='otp-code'>{otp}</div>
            
            <div class='warning'>
                <strong>⚠️ Security Notice:</strong>
                <ul>
                    <li>This code expires in <strong>10 minutes</strong></li>
                    <li>Never share this code with anyone</li>
                    <li>DesiCorner staff will never ask for your OTP</li>
                </ul>
            </div>
            
            <p>If you didn't request this code, please ignore this email or contact our support team.</p>
        </div>
        <div class='footer'>
            <p>© 2024 DesiCorner. All rights reserved.</p>
            <p>This is an automated message, please do not reply.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(to, subject, body, ct);
    }
}