using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Identity;

public sealed record RegisteredAccount(int AccountId, string EmailVerificationToken);
