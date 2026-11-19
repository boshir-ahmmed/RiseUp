using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Investors;

public sealed record InvestorDetailDto(InvestorCardDto Card, IReadOnlyList<(string Startup, int Year, string Round, bool Confirmed)> Claims,
    string? ContactEmail, string? ContactPhone, bool ViewerIsSelf, bool ViewerConnected, bool ViewerCanPitch, VerificationStatus Status);
