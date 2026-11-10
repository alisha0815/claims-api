using System.ComponentModel.DataAnnotations;

namespace ClaimsApi.Validation;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class NotInFutureAttribute : ValidationAttribute
{
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;

    public NotInFutureAttribute()
        : base("The {0} field cannot be in the future.") { }

    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        if (value is not DateOnly date)
            throw new InvalidOperationException(
                "NotInFutureAttribute can only be used on DateOnly values."
            );

        var today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);

        return date <= today;
    }
}
