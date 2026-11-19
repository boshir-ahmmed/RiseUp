using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Connections;
using RiseUp.Entities.Conversations;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.Conversations;

public sealed record ConversationDto(ConversationSummaryDto Summary, IReadOnlyList<string> Members, IReadOnlyList<MessageDto> Messages, string? ContactEmail, string? ContactPhone);
