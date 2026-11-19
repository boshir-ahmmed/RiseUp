using FluentValidation;
using RiseUp.Entities.Common;
using RiseUp.Framework.Constants;

namespace RiseUp.Facade.Pitching;

public sealed record PitchPackageDto(int StartupId, string StartupName, int Version, VideoDto? Pitch, VideoDto? Demo, VideoDto? Teaser, bool PublicTeaser, bool PublicDemo,
    bool Captions, FileDto? DeckFile, IReadOnlyList<SlideDto> Slides, IReadOnlyList<PitchDocumentDto> Documents, AskDto Ask, IReadOnlyList<string> Highlights,
    IReadOnlyList<TeamDto> Team, IReadOnlyList<FaqDto> Faq, int Completeness, IReadOnlyList<(string Item, string Hint, bool Done)> Checklist);
