using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record FileDto(int MediaId, string FileName, long SizeBytes, ScanStatus Scan, string Url);
