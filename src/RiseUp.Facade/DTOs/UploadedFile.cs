using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Media;

public sealed record UploadedFile(string StorageKey, string FileName, string ContentType, long SizeBytes, int? DurationSeconds = null);
