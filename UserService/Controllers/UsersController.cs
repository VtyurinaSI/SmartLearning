using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserService
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserProgressRepository _repo;
        private readonly IUserContext _userContext;
        private readonly IUserRoleService _roleService;

        public UsersController(IUserProgressRepository repo, IUserContext userContext, IUserRoleService roleService)
        {
            _repo = repo;
            _userContext = userContext;
            _roleService = roleService;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserProfile>> GetMe(CancellationToken ct)
        {
            if (!_userContext.TryGetUserId(User, out var userId))
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
            if (!_userContext.TryGetUserId(User, out var userId))
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
            if (!_userContext.TryGetUserId(User, out var callerId))
            {
                return Unauthorized();
            }

            if (callerId != id && !await _roleService.IsAdminAsync(callerId, ct))
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
            if (!_userContext.TryGetUserId(User, out var callerId))
            {
                return Unauthorized();
            }

            if (!await _roleService.IsAdminAsync(callerId, ct))
            {
                return Forbid();
            }

            if (!_roleService.TryNormalizeRole(request?.Role, out var normalizedRole))
            {
                return BadRequest(new { message = "Role must be 'user' or 'admin'." });
            }

            var target = await _repo.GetUserProfileAsync(id, ct);
            if (target == null)
            {
                return NotFound();
            }

            await _repo.SetUserRoleAsync(id, normalizedRole, ct);
            return NoContent();
        }
    }
}
