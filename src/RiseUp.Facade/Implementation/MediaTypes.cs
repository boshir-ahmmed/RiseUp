using Microsoft.Extensions.Logging;
using RiseUp.Entities.Abstractions;
using RiseUp.Entities.Common;
using RiseUp.Entities.Media;
using RiseUp.Framework.Constants;
using RiseUp.Framework.Results;

namespace RiseUp.Facade.Media;

public static class MediaTypes
{
    public static readonly string[] Images = ["image/jpeg", "image/png", "image/webp"];
    public static readonly string[] Videos = ["video/mp4", "video/webm", "video/quicktime"];
    public const string Pdf = "application/pdf";
    public const string Pptx = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
    public const string Xlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
