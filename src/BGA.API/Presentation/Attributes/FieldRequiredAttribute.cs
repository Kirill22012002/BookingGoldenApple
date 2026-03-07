using System.ComponentModel.DataAnnotations;

namespace BGA.API.Presentation.Attributes;

public sealed class FieldRequiredAttribute : ValidationAttribute
{
    public override string FormatErrorMessage(string fieldName)
    {
        return !string.IsNullOrEmpty(ErrorMessage)
            ? ErrorMessage
            : $"The {fieldName} field is required";
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return false;
        }

        Type type = value.GetType();
        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            if (Equals(value, defaultValue))
            {
                return false;
            }
        }

        return true;
    }
}
