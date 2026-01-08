using System.Security.Claims;

namespace UserService
{
    public interface IUserContext
    {
        bool TryGetUserId(ClaimsPrincipal user, out Guid userId);
    }
}
