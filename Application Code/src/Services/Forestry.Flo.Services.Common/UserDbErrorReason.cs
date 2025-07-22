using System.ComponentModel;

namespace Forestry.Flo.Services.Common;

public enum UserDbErrorReason
{
    [Description("Not found in database")]
    NotFound,
    [Description("No unique")]
    NotUnique,
    [Description("General error")]
    General
}