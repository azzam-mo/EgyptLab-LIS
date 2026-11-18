using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EgyptLab.LIS.Infrastructure.Domain;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EgyptLab.LIS.Api.Identity;

public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);

/// <summary>
/// Creates short-lived access tokens from the approved custom user store.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public IssuedToken Create(User user, IEnumerable<string> permissions)
    {
        var settings = options.Value;
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(settings.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.NormalizedUserName),
            new(LisClaims.DisplayName, user.FullName),
            new(LisClaims.BranchId, user.BranchId.ToString()),
            new(LisClaims.SecurityStamp, user.SecurityStamp)
        };

        claims.AddRange(permissions.Select(permission => new Claim(LisClaims.Permission, permission)));

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
