using System.ComponentModel.DataAnnotations;

namespace BGA.API.Attributes;

public sealed class DateTimeGreaterThanAttribute : ValidationAttribute
{
    private readonly string _targetPropertyName;

    public DateTimeGreaterThanAttribute(string targetPropertyName) : base()
    {
        _targetPropertyName = targetPropertyName;
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        var targetProperty = validationContext.ObjectType.GetProperty(_targetPropertyName);
        var targetPropertyValue = targetProperty?.GetValue(validationContext.ObjectInstance, null);

        if (targetPropertyValue is DateTime startAt && value is DateTime endAt)
        {
            if (endAt <= startAt)
            {
                return new ValidationResult($" must be greater than the {nameof(_targetPropertyName)}");
            }
            else
            {
                return null;
            }

        }

        return null;
    }
}
