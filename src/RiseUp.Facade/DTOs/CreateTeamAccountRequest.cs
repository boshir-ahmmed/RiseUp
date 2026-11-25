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

public sealed record CreateTeamAccountRequest(string FullName, string Designation, string Username, string Email, string? LinkedInUrl,
    string? Password = null, TeamGroupMode GroupMode = TeamGroupMode.Selected, int[]? GroupIds = null);
