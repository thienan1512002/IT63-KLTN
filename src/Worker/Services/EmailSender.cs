using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Worker.Data;
using Worker.Services.Handlers;
using Worker.Settings;
using Worker.Shared.Data;
using Worker.Shared.Dtos;
using Worker.Shared.Entities;
using Worker.Shared.Enums;

namespace Worker.Services;

public class EmailSender(
    IOptions<SmtpSettings> smtpOptions,
    WorkerDbContext dbContext,
    NotificationEmailDispatcher dispatcher,
    ILogger<EmailSender> logger) : IEmailSender
{
    private readonly SmtpSettings _smtp = smtpOptions.Value;

    public async Task<long> CreateMasterDataAsync(EmailRequest request)
    {
        var masterData = new EmailMasterData
        {
            NotificationType = request.NotificationType,
            WorkflowAction = request.WorkflowAction,
            ToAddress = request.ToAddress,
            CcAddresses = request.CcAddresses.Count > 0
                ? string.Join(";", request.CcAddresses)
                : null,
            Subject = request.Subject,
            TemplateDataJson = request.TemplateData.Count > 0
                ? JsonSerializer.Serialize(request.TemplateData)
                : null,
            Status = EmailMasterDataStatus.Pending,
            CreatedAt = DateTime.Now
        };

        dbContext.EmailMasterDatas.Add(masterData);        
       await dbContext.SaveChangesAsync();

        return masterData.Id;
    }

    public async Task SendAsync(long masterDataId)
    {
        var masterData = await dbContext.EmailMasterDatas.FindAsync(masterDataId)
            ?? throw new InvalidOperationException($"EmailMasterData not found: {masterDataId}");

        try
        {
            masterData.AttemptNumber++;
            var body = dispatcher.BuildBody(masterData);

            using var message = BuildMailMessage(masterData, body);
            using var client = BuildSmtpClient();

            await client.SendMailAsync(message);

            masterData.Status = EmailMasterDataStatus.Sent;
            masterData.SentAt = DateTime.Now;

            dbContext.EmailLogs.Add(new EmailLog
            {
                EmailMasterDataId = masterDataId,
                IsSuccess = true,
                CreatedAt = DateTime.Now
            });

            logger.LogInformation("Email sent [MasterDataId={Id}, To={To}]",
                masterData.Id, masterData.ToAddress);
        }
        catch (Exception ex)
        {
            masterData.Status = EmailMasterDataStatus.Failed;
            masterData.RetryAt = DateTime.Now;

            dbContext.EmailLogs.Add(new EmailLog
            {
                EmailMasterDataId = masterDataId,
                IsSuccess = false,
                ErrorMessage = ex.ToString(),
                CreatedAt = DateTime.Now
            });

            logger.LogError(ex, "Failed to send email [MasterDataId={Id}, To={To}]",
                masterData.Id, masterData.ToAddress);

            throw;
        }
        finally
        {
            await dbContext.SaveChangesAsync();
        }
    }

    private MailMessage BuildMailMessage(EmailMasterData masterData, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
            Subject = masterData.Subject,
            Body = body,
            IsBodyHtml = true
        };

        var html = AlternateView.CreateAlternateViewFromString(body, System.Text.Encoding.UTF8, MediaTypeNames.Text.Html);
        var logo = new LinkedResource(Path.Combine(AppContext.BaseDirectory, "Assets", "nam-viet-mark.png"), MediaTypeNames.Image.Png)
        {
            ContentId = "nam-viet-logo",
            TransferEncoding = TransferEncoding.Base64
        };
        html.LinkedResources.Add(logo);
        message.AlternateViews.Add(html);

        message.To.Add(masterData.ToAddress);

        if (!string.IsNullOrWhiteSpace(masterData.CcAddresses))
        {
            foreach (var cc in masterData.CcAddresses.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                message.CC.Add(cc.Trim());
            }
        }

        return message;
    }

    private SmtpClient BuildSmtpClient()
    {
        if (!string.IsNullOrWhiteSpace(_smtp.PickupDirectory))
        {
            var path = Path.GetFullPath(_smtp.PickupDirectory);
            Directory.CreateDirectory(path);
            return new SmtpClient {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = path,
                EnableSsl = false
            };
        }
        return new SmtpClient(_smtp.Host, _smtp.Port)
        {
            Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
            EnableSsl = _smtp.EnableSsl
        };
    }
}
