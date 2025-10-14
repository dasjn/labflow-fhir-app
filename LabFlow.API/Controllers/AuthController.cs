using LabFlow.API.Data.Entities;
using LabFlow.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LabFlow.API.Controllers;

/// <summary>
/// Authentication controller for JWT-based user management
/// REQ-FHIR-006: Implement secure authentication for FHIR API access
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration details</param>
    /// <returns>User ID and confirmation</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        _logger.LogInformation("POST /Auth/register - New user registration");

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ErrorResponse { Error = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse { Error = "Password is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest(new ErrorResponse { Error = "Role is required" });
        }

        // Register user
        var result = await _authService.RegisterAsync(
            request.Email,
            request.Password,
            request.Role,
            request.FullName);

        if (!result.Success)
        {
            _logger.LogWarning("Registration failed for email: {Email}, error: {Error}",
                request.Email, result.ErrorMessage);
            return BadRequest(new ErrorResponse { Error = result.ErrorMessage! });
        }

        _logger.LogInformation("User registered successfully: UserId={UserId}", result.UserId);

        return CreatedAtAction(
            nameof(Register),
            new RegisterResponse
            {
                UserId = result.UserId!,
                Email = request.Email,
                Role = request.Role,
                Message = "User registered successfully"
            });
    }

    /// <summary>
    /// Login and get JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token with expiration</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("POST /Auth/login - Login attempt for email: {Email}", request.Email);

        // Validate request
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ErrorResponse { Error = "Email is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ErrorResponse { Error = "Password is required" });
        }

        // Authenticate user
        var result = await _authService.LoginAsync(request.Email, request.Password);

        if (!result.Success)
        {
            _logger.LogWarning("Login failed for email: {Email}", request.Email);
            return Unauthorized(new ErrorResponse { Error = "Invalid email or password" });
        }

        _logger.LogInformation("Login successful for email: {Email}", request.Email);

        return Ok(new LoginResponse
        {
            Token = result.Token!,
            TokenType = "Bearer",
            ExpiresIn = result.ExpiresIn!.Value,
            Message = "Login successful"
        });
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        // Extract user info from JWT claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                    ?? User.FindFirst("email")?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
                   ?? User.FindFirst("role")?.Value;
        var fullName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        _logger.LogInformation("GET /Auth/me - User info requested by UserId: {UserId}", userId);

        return Ok(new UserInfoResponse
        {
            UserId = userId!,
            Email = email!,
            Role = role!,
            FullName = fullName
        });
    }
}

#region Request/Response DTOs

/// <summary>
/// Registration request
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

/// <summary>
/// Registration response
/// </summary>
public class RegisterResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Login request
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Login response with JWT token
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // Seconds
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Current user info response
/// </summary>
public class UserInfoResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
}

/// <summary>
/// Error response
/// </summary>
public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
}

#endregion
