using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserService
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            "user",
            "admin"
        };

        private readonly IUserProgressRepository _repo;

        public UsersController(IUserProgressRepository repo)
        {
            _repo = repo;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> GetMe(CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            var profile = await _repo.GetUserProfileAsync(userId, ct);
            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPatch("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
            {
                return Unauthorized();
            }

            if (request == null)
            {
                return BadRequest();
            }

            if (request.Location == null && request.ProgrammingLanguage == null)
            {
                return BadRequest(new { message = "At least one field must be provided." });
            }

            var updated = await _repo.UpdateUserProfileAsync(userId, request.Location, request.ProgrammingLanguage, ct);
            if (!updated)
            {
                return NotFound();
            }

            var profile = await _repo.GetUserProfileAsync(userId, ct);
            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> GetUser(Guid id, CancellationToken ct)
        {
            if (!TryGetUserId(out var callerId))
            {
                return Unauthorized();
            }

            if (callerId != id && !await IsAdminAsync(callerId, ct))
            {
                return Forbid();
            }

            var profile = await _repo.GetUserProfileAsync(id, ct);
            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPut("{id:guid}/role")]
        [Authorize]
        public async Task<IActionResult> SetRole(Guid id, [FromBody] SetUserRoleRequest request, CancellationToken ct)
        {
            if (!TryGetUserId(out var callerId))
            {
                return Unauthorized();
            }

            if (!await IsAdminAsync(callerId, ct))
            {
                return Forbid();
            }

            var role = request?.Role?.Trim();
            if (string.IsNullOrWhiteSpace(role) || !AllowedRoles.Contains(role))
            {
                return BadRequest(new { message = "Role must be 'user' or 'admin'." });
            }

            var target = await _repo.GetUserProfileAsync(id, ct);
            if (target == null)
            {
                return NotFound();
            }

            await _repo.SetUserRoleAsync(id, role.ToLowerInvariant(), ct);
            return NoContent();
        }

        private bool TryGetUserId(out Guid userId)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue("sub");

            return Guid.TryParse(sub, out userId);
        }

        private async Task<bool> IsAdminAsync(Guid userId, CancellationToken ct)
        {
            var role = await _repo.GetUserRoleAsync(userId, ct);
            return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
