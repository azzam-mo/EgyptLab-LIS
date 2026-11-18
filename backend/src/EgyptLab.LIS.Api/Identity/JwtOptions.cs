using System.Security.Cryptography;

namespace EgyptLab.LIS.Api.Identity;

/// <summary>
/// Signing configuration. Production must supply a persistent key; development
/// generates an ephemeral key so no secret is committed to the repository.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public static string DevelopmentSigningKey { get; } =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public string Issuer { get; set; } = "egyptlab-lis";

    public string Audience { get; set; } = "egyptlab-lis-clients";

    public string SigningKey { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 15;
}
