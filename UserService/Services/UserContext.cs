using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UserService
{
    public sealed class UserContext : IUserContext
    {
        public bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
        {
            var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? user.FindFirstValue("sub");

            return Guid.TryParse(sub, out userId);
        }
    }
}
