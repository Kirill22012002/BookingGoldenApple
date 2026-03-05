using System.ComponentModel.DataAnnotations;

namespace BGA.API.Presentation.Attributes;

public sealed class FieldRequiredAttribute : RequiredAttribute
{
    public override string FormatErrorMessage(string fieldName)
    {
        return !string.IsNullOrEmpty(ErrorMessage)
            ? ErrorMessage
            : $"The {fieldName} field is required";
    }
}
