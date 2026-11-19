using FluentValidation;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Conversations;
using RiseUp.Entities.Startups;
using RiseUp.Facade.Common;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;
using RiseUp.Framework.Time;

namespace RiseUp.Facade.TeamAccounts;


/// <summary>The plaintext password exists only in this response and is never stored, logged or emailed (FR-046).</summary>
public sealed record OneTimeCredential(int TeamAccountId, string Username, string TemporaryPassword);
