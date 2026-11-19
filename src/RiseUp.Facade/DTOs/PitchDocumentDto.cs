using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record PitchDocumentDto(int Id, int MediaId, string FileName, string Label, long SizeBytes, ScanStatus Scan, bool AllowDownload, string Url);
