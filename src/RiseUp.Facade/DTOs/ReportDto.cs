using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Governance;
using RiseUp.Facade.Common;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Moderation;

public sealed record ReportDto(int Id, ReportKind Kind, string Target, string Reason, ReportStatus Status, DateTime AtUtc, string? Resolution);
