using System.ComponentModel.DataAnnotations;

namespace BGA.Users.API.Attributes;

public sealed class FieldRequiredAttribute : ValidationAttribute
{
    public override string FormatErrorMessage(string fieldName)
    {
        return !string.IsNullOrEmpty(ErrorMessage)
            ? ErrorMessage
            : $"The field {fieldName} is required";
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
        {
            return false;
        }

        var type = value.GetType();
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
