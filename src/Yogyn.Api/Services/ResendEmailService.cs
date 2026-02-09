using Resend;
using Yogyn.Api.Events;

namespace Yogyn.Api.Services;

public class ResendEmailService : IEmailService
{
    private readonly IResend _resend;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly EmailTemplateLoader _templateLoader;
    private readonly string _fromAddress;

    public ResendEmailService(
        IResend resend,
        EmailTemplateLoader templateLoader,
        IConfiguration configuration,
        ILogger<ResendEmailService> logger)
    {
        _resend = resend;
        _templateLoader = templateLoader;
        _logger = logger;
        _fromAddress = configuration["Email:FromAddress"] ?? "bookings@yogyn.com";
    }

    public async Task SendBookingConfirmationAsync(BookingCreatedEvent booking)
    {
        var welcomeMessage = booking.IsReturningCustomer
            ? "<p>Welcome back, {0}!</p>"
            : "<p>Hi {0},</p>";
        
        welcomeMessage = string.Format(welcomeMessage, booking.FirstName);

        var placeholders = new Dictionary<string, string>
        {
            { "WelcomeMessage", welcomeMessage },
            { "FirstName", booking.FirstName },
            { "SessionTitle", booking.SessionTitle },
            { "StudioName", booking.StudioName },
            { "SessionDateTime", booking.SessionStartsAt.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") },
            { "Duration", booking.SessionDuration.ToString() },
            { "CancelUrl", $"https://yogyn.com/api/bookings/cancel/{booking.CancelToken}" }
        };

        var html = _templateLoader.RenderTemplate("confirmation", placeholders);
        await SendEmailAsync(booking.Email, $"Booking Confirmed - {booking.SessionTitle}", html, booking.BookingId);
    }

    public async Task SendBookingPendingAsync(BookingCreatedEvent booking)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", booking.FirstName },
            { "SessionTitle", booking.SessionTitle },
            { "StudioName", booking.StudioName },
            { "SessionDateTime", booking.SessionStartsAt.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") },
            { "Duration", booking.SessionDuration.ToString() }
        };

        var html = _templateLoader.RenderTemplate("pending", placeholders);
        await SendEmailAsync(booking.Email, $"Booking Received - {booking.SessionTitle}", html, booking.BookingId);
    }

    public async Task SendBookingApprovedAsync(BookingApprovedEvent booking)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", booking.FirstName },
            { "SessionTitle", booking.SessionTitle },
            { "StudioName", booking.StudioName },
            { "SessionDateTime", booking.SessionStartsAt.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") },
            { "Duration", booking.SessionDuration.ToString() },
            { "CancelUrl", $"https://yogyn.com/api/bookings/cancel/{booking.CancelToken}" }
        };

        var html = _templateLoader.RenderTemplate("approved", placeholders);
        await SendEmailAsync(booking.Email, $"Booking Approved - {booking.SessionTitle}", html, booking.BookingId);
    }

    public async Task SendBookingRejectedAsync(BookingRejectedEvent booking)
    {
        var reasonSection = !string.IsNullOrWhiteSpace(booking.Reason)
            ? $"<p><strong>Reason:</strong> {booking.Reason}</p>"
            : "";

        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", booking.FirstName },
            { "SessionTitle", booking.SessionTitle },
            { "StudioName", booking.StudioName },
            { "SessionDateTime", booking.SessionStartsAt.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") },
            { "ReasonSection", reasonSection }
        };

        var html = _templateLoader.RenderTemplate("rejected", placeholders);
        await SendEmailAsync(booking.Email, $"Booking Not Approved - {booking.SessionTitle}", html, booking.BookingId);
    }

    public async Task SendBookingCancelledAsync(BookingCancelledEvent booking)
    {
        var placeholders = new Dictionary<string, string>
        {
            { "FirstName", booking.FirstName },
            { "SessionTitle", booking.SessionTitle },
            { "StudioName", booking.StudioName },
            { "SessionDateTime", booking.SessionStartsAt.ToString("dddd, MMMM dd, yyyy 'at' h:mm tt") }
        };

        var html = _templateLoader.RenderTemplate("cancelled", placeholders);
        await SendEmailAsync(booking.Email, $"Booking Cancelled - {booking.SessionTitle}", html, booking.BookingId);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string html, Guid bookingId)
    {
        try
        {
            var message = new EmailMessage
            {
                From = _fromAddress,
                To = toEmail,
                Subject = subject,
                HtmlBody = html
            };

            await _resend.EmailSendAsync(message);
            
            _logger.LogInformation("Email sent successfully to {Email} for booking {BookingId}", toEmail, bookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to send email to {Email} for booking {BookingId}. Subject: {Subject}", 
                toEmail, 
                bookingId,
                subject
            );
        }
    }
}
