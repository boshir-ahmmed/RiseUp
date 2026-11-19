using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record VideoDto(int MediaId, string Url, string? PosterUrl, int Seconds, ScanStatus Scan);
