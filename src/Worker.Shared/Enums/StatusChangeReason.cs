namespace Worker.Shared.Enums;

public enum StatusChangeReason
{
    ByScheduledDeactivation = 1,
    ByImmediateDeactivation = 2,
    ByManualOverride = 3,
    ByScheduledReactivation = 4,
}
