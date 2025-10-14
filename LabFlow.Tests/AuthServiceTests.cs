using FluentAssertions;
using LabFlow.API.Configuration;
using LabFlow.API.Data;
using LabFlow.API.Data.Entities;
using LabFlow.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Task = System.Threading.Tasks.Task;

namespace LabFlow.Tests;

/// <summary>
/// Unit tests for AuthService (JWT authentication)
/// Tests registration, login, token generation, and validation
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly FhirDbContext _context;
    private readonly AuthService _authService;
    private readonly JwtSettings _jwtSettings;
    private readonly Mock<ILogger<AuthService>> _loggerMock;

    public AuthServiceTests()
    {
        // Create in-memory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<FhirDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new FhirDbContext(options);

        // Setup JWT settings for testing
        _jwtSettings = new JwtSettings
        {
            SecretKey = "ThisIsATestSecretKeyWithAtLeast32CharactersForHS256",
            Issuer = "TestLabFlowAPI",
            Audience = "TestLabFlowClients",
            ExpirationMinutes = 60,
            AuthProvider = "JWT"
        };

        _loggerMock = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_context, _jwtSettings, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region Registration Tests

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsSuccess()
    {
        // Arrange
        var email = "test@hospital.com";
        var password = "SecurePass123!";
        var role = UserRoles.Doctor;
        var fullName = "Dr. Test";

        // Act
        var result = await _authService.RegisterAsync(email, password, role, fullName);

        // Assert
        result.Success.Should().BeTrue();
        result.UserId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();

        // Verify user was saved to database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(email.ToLower());
        savedUser.Role.Should().Be(role);
        savedUser.FullName.Should().Be(fullName);
        savedUser.PasswordHash.Should().NotBeNullOrEmpty();
        savedUser.PasswordHash.Should().StartWith("$2a$"); // BCrypt hash format
        savedUser.IsActive.Should().BeTrue();
        savedUser.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var email = "duplicate@hospital.com";
        await _authService.RegisterAsync(email, "Password123!", UserRoles.Doctor);

        // Act - Try to register same email again
        var result = await _authService.RegisterAsync(email, "DifferentPass123!", UserRoles.LabTechnician);

        // Assert
        result.Success.Should().BeFalse();
        result.UserId.Should().BeNull();
        result.ErrorMessage.Should().Be("Email already exists");
    }

    [Fact]
    public async Task RegisterAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var invalidEmail = "not-an-email";

        // Act
        var result = await _authService.RegisterAsync(invalidEmail, "Password123!", UserRoles.Doctor);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email format");
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ReturnsFailure()
    {
        // Arrange
        var shortPassword = "short";

        // Act
        var result = await _authService.RegisterAsync("test@hospital.com", shortPassword, UserRoles.Doctor);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Password must be at least 8 characters");
    }

    [Fact]
    public async Task RegisterAsync_InvalidRole_ReturnsFailure()
    {
        // Arrange
        var invalidRole = "InvalidRole";

        // Act
        var result = await _authService.RegisterAsync("test@hospital.com", "Password123!", invalidRole);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid role");
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var email = "login@hospital.com";
        var password = "SecurePass123!";
        await _authService.RegisterAsync(email, password, UserRoles.Doctor);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Success.Should().BeTrue();
        result.Token.Should().NotBeNullOrEmpty();
        result.Token.Should().Contain("."); // JWT has 3 parts separated by dots
        result.ExpiresIn.Should().Be(3600); // 60 minutes * 60 seconds
        result.ErrorMessage.Should().BeNull();

        // Verify LastLoginAt was updated
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        user!.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginAsync_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var nonExistentEmail = "nonexistent@hospital.com";

        // Act
        var result = await _authService.LoginAsync(nonExistentEmail, "AnyPassword123!");

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ErrorMessage.Should().Be("Invalid email or password"); // Don't reveal if user exists
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        var email = "wrongpass@hospital.com";
        var correctPassword = "CorrectPass123!";
        await _authService.RegisterAsync(email, correctPassword, UserRoles.Doctor);

        // Act
        var result = await _authService.LoginAsync(email, "WrongPassword123!");

        // Assert
        result.Success.Should().BeFalse();
        result.Token.Should().BeNull();
        result.ErrorMessage.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsFailure()
    {
        // Arrange
        var email = "inactive@hospital.com";
        var password = "Password123!";
        await _authService.RegisterAsync(email, password, UserRoles.Doctor);

        // Deactivate user
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        user!.IsActive = false;
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Account is disabled");
    }

    #endregion

    #region Token Validation Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsUserId()
    {
        // Arrange
        var email = "tokentest@hospital.com";
        var password = "Password123!";
        var registerResult = await _authService.RegisterAsync(email, password, UserRoles.Doctor);
        var loginResult = await _authService.LoginAsync(email, password);

        // Act - Use same AuthService instance (same JwtSettings)
        var userId = await _authService.ValidateTokenAsync(loginResult.Token!);

        // Assert
        userId.Should().NotBeNull();
        userId.Should().Be(registerResult.UserId);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var userId = await _authService.ValidateTokenAsync(invalidToken);

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenAsync_ExpiredToken_ReturnsNull()
    {
        // Arrange - Create settings with 0 expiration
        var expiredJwtSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = 0, // Token expires immediately
            AuthProvider = "JWT"
        };

        var expiredAuthService = new AuthService(_context, expiredJwtSettings, _loggerMock.Object);

        var email = "expired@hospital.com";
        var password = "Password123!";
        await expiredAuthService.RegisterAsync(email, password, UserRoles.Doctor);
        var loginResult = await expiredAuthService.LoginAsync(email, password);

        // Wait a moment to ensure token is expired
        await Task.Delay(1000);

        // Act
        var userId = await _authService.ValidateTokenAsync(loginResult.Token!);

        // Assert
        userId.Should().BeNull();
    }

    #endregion

    #region Password Security Tests

    [Fact]
    public async Task RegisterAsync_PasswordIsHashed_NotStoredInPlainText()
    {
        // Arrange
        var email = "hash@hospital.com";
        var password = "PlainTextPassword123!";

        // Act
        await _authService.RegisterAsync(email, password, UserRoles.Doctor);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
        user!.PasswordHash.Should().NotBe(password); // Never store plain text
        user.PasswordHash.Should().StartWith("$2a$"); // BCrypt format
        user.PasswordHash.Length.Should().Be(60); // BCrypt always 60 characters
    }

    [Fact]
    public async Task RegisterAsync_SamePassword_ProducesDifferentHashes()
    {
        // Arrange
        var password = "SamePassword123!";
        var email1 = "user1@hospital.com";
        var email2 = "user2@hospital.com";

        // Act
        await _authService.RegisterAsync(email1, password, UserRoles.Doctor);
        await _authService.RegisterAsync(email2, password, UserRoles.LabTechnician);

        // Assert - BCrypt includes random salt, so same password = different hash
        var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Email == email1.ToLower());
        var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Email == email2.ToLower());

        user1!.PasswordHash.Should().NotBe(user2!.PasswordHash);
    }

    #endregion

    #region JWT Token Content Tests

    [Fact]
    public async Task LoginAsync_TokenContainsExpectedClaims()
    {
        // Arrange
        var email = "claims@hospital.com";
        var password = "Password123!";
        var role = UserRoles.Doctor;
        var fullName = "Dr. Claims Test";
        await _authService.RegisterAsync(email, password, role, fullName);

        // Act
        var result = await _authService.LoginAsync(email, password);

        // Assert
        var token = result.Token!;
        var parts = token.Split('.');
        parts.Length.Should().Be(3); // Header.Payload.Signature

        // Decode payload (base64)
        var payload = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));

        // Check expected claims
        payload.Should().Contain("\"email\":\"claims@hospital.com\"");
        payload.Should().Contain("\"role\":\"Doctor\"");
        payload.Should().Contain("\"iss\":\"TestLabFlowAPI\"");
        payload.Should().Contain("\"aud\":\"TestLabFlowClients\"");
        payload.Should().Contain("\"fhirUser\"");
        payload.Should().Contain("\"scope\"");
    }

    #endregion
}
