namespace EgyptLab.LIS.Api.Tests;

/// <summary>
/// Synthetic JWT settings shared by authentication and token-validation tests.
/// No production secret or real identity is stored here.
/// </summary>
internal static class JwtTestConfiguration
{
    internal const string Issuer = "egyptlab-tests";

    internal const string Audience = "egyptlab-test-clients";

    // Longer than 48 bytes so the same key can also sign the HS384 negative case.
    internal const string SigningKey = "integration-test-signing-key-64-bytes-long-for-hs384-negative-case-testing!";

    internal const int ExpiryMinutes = 15;
}
