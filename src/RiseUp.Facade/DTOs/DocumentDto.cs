using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.ConfidentialDocuments;

public sealed record DocumentDto(int Id, string FileName, DocumentKind Kind, long SizeBytes, ScanStatus Scan, IReadOnlyList<int> GrantedInvestorIds);
