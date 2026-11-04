using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Worker.Settings;

public sealed class SmtpSettingsValidator : IValidateOptions<SmtpSettings>
{
    public ValidateOptionsResult Validate(string name, SmtpSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.PickupDirectory)) return ValidateOptionsResult.Success;
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.Host)) errors.Add("Smtp:Host chưa được cấu hình.");
        if (settings.Port is < 1 or > 65535) errors.Add("Smtp:Port phải nằm trong khoảng 1–65535.");
        if (!MailAddress.TryCreate(settings.FromAddress, out var sender) || !sender.Address.Contains('@'))
            errors.Add("Smtp:FromAddress phải là địa chỉ email gửi hợp lệ.");
        if (string.Equals(settings.Host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            if (settings.Port != 587 || !settings.EnableSsl)
                errors.Add("Gmail cần cổng 587 và Smtp:EnableSsl=true (STARTTLS).");
            if (!MailAddress.TryCreate(settings.Username, out var account) || !account.Address.Contains('@'))
                errors.Add("Smtp:Username phải là địa chỉ Gmail/Google Workspace đầy đủ.");
            if (string.IsNullOrWhiteSpace(settings.Password))
                errors.Add("Thiếu mật khẩu ứng dụng Gmail. Điền Smtp:Username, Smtp:Password và Smtp:FromAddress trong appsettings hoặc cấu hình riêng của Worker.");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
