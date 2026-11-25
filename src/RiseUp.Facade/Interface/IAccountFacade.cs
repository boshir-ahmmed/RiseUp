using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Identity;
using RiseUp.Entities.Investors;
using RiseUp.Entities.Startups;
using RiseUp.Entities.Wallets;
using RiseUp.Facade.Common;
using RiseUp.Facade.Promotions;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Identity;

public interface IAccountFacade
{
    Task<Result<RegisteredAccount>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result> ResendVerificationAsync(string email, CancellationToken ct = default);
    Task<Result> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task<Result<SignedInUser>> SignInAsync(string identifier, string password, CancellationToken ct = default);
    Task<Result<SignedInUser>> SignInTeamMemberAsync(string identifier, string password, CancellationToken ct = default);
    Task<bool> IsTeamSessionCurrentAsync(int memberId, int version, CancellationToken ct = default);
    Task<Result> VerifyMfaAsync(int accountId, string code, CancellationToken ct = default);
    Task<Result<string>> RequestPasswordResetAsync(string email, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(string token, string newPassword, CancellationToken ct = default);
    Task<Result> DeleteAccountAsync(Actor actor, string confirmation, CancellationToken ct = default);
}
