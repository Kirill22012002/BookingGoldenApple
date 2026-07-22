using System.ComponentModel.DataAnnotations;

namespace BGA.Events.API.Attributes;

public sealed class GreaterThanAttribute<T>(string targetPropertyName) : ValidationAttribute where T : IComparable<T>
{
    protected override ValidationResult? IsValid(object? sourceObject, ValidationContext validationContext)
    {
        var targetProperty = validationContext.ObjectType.GetProperty(targetPropertyName);
        var targetObject = targetProperty?.GetValue(validationContext.ObjectInstance, null);

        if (targetObject is T target && sourceObject is T source && target.CompareTo(source) >= 0)
        {
            return new ValidationResult($"{validationContext.DisplayName} must be greater than the {targetPropertyName}");
        }

        return ValidationResult.Success;
    }
}
