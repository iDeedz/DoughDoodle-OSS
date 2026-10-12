using DoughDoodle.Models;
using DoughDoodle.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DoughDoodle.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class ApiAuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IJwtTokenService _jwt;

    public ApiAuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IJwtTokenService jwt)
    {
        _users = users;
        _signIn = signIn;
        _jwt = jwt;
    }

    public record LoginRequest(string Email, string Password);

    [HttpPost("token")]
    [AllowAnonymous]
    public async Task<IActionResult> Token([FromBody] LoginRequest req)
    {
        var user = await _users.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized(new { error = "Invalid credentials" });
        var result = await _signIn.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return Unauthorized(new { error = "Invalid credentials" });
        var token = _jwt.CreateToken(user);
        return Ok(new { access_token = token, token_type = "Bearer", expires_in_minutes = 60 });
    }
}
