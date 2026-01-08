using AuthService.DTOs;
using AuthService.Models;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using SmartLearning.Contracts;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<User> _userManager;
        private readonly IPublishEndpoint _publish;
        private readonly IJwtTokenService _tokenService;

        public AuthService(
            UserManager<User> userManager,
            IPublishEndpoint publish,
            IJwtTokenService tokenService)
        {
            _userManager = userManager;
            _publish = publish;
            _tokenService = tokenService;
        }

        public async Task<string> RegisterAsync(RegisterRequest request)
        {
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = request.Email,
                UserName = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            await _publish.Publish(new UserCreated(Guid.Parse(user.Id), user.UserName!, user.Email!), default);
            return _tokenService.GenerateToken(user);
        }

        public async Task<string> LoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            {
                throw new Exception("Invalid credentials");
            }

            return _tokenService.GenerateToken(user);
        }
    }
}
