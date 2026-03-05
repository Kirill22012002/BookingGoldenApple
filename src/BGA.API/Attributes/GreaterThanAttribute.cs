using System.ComponentModel.DataAnnotations;

namespace BGA.API.Attributes;

public sealed class GreaterThanAttribute<T>(string _targetPropertyName) : ValidationAttribute where T : IComparable<T>
{
    protected override ValidationResult? IsValid(object? sourceObj, ValidationContext validationContext)
    {
        var targetProperty = validationContext.ObjectType.GetProperty(_targetPropertyName);
        var targetObj = targetProperty?.GetValue(validationContext.ObjectInstance, null);

        if (targetObj is T target && sourceObj is T source)
            if (target.CompareTo(source) >= 0)
                return new ValidationResult($"{validationContext.DisplayName} must be greater than the {_targetPropertyName}");

        return ValidationResult.Success;
    }
}
