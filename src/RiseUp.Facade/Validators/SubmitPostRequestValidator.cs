using FluentValidation;
using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Promotions;
using RiseUp.Entities.Social;
using RiseUp.Facade.Common;
using RiseUp.Facade.Discovery;
using RiseUp.Facade.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Posts;

public sealed class SubmitPostRequestValidator : AbstractValidator<SubmitPostRequest>
{
    public SubmitPostRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(BusinessRules.PostTitleMax);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(BusinessRules.PostBodyMax);
        RuleFor(x => x.Category).IsInEnum();
    }
}
