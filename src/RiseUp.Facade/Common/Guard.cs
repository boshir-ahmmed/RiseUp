using FluentValidation;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Common;

internal static class Guard
{
    public static async Task<Error?> ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        return result.IsValid ? null : new Error(ErrorCodes.Validation, string.Join(" ", result.Errors.Select(e => e.ErrorMessage).Distinct()));
    }

    public static Error Forbidden(string message = "You do not have permission to do this.") => new(ErrorCodes.Forbidden, message);
    public static Error NotFound(string what = "The item") => new(ErrorCodes.NotFound, $"{what} was not found.");
}
