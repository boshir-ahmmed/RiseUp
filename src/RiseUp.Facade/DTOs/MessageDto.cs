using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Conversations;

public sealed record MessageDto(int Id, string AuthorName, string Body, DateTime SentAtUtc, bool IsMine, bool IsSystem, bool Edited, bool Deleted, bool CanEdit, bool CanDelete, string? AuthorPhotoUrl = null, int? AttachmentId = null, string? FileName = null);
