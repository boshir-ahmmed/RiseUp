using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Moderation;

public sealed record AuditDto(DateTime AtUtc, string ActorRole, string Action, string Target, string Reason, bool Restricted);
