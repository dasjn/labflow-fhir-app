using FluentAssertions;
using LabFlow.API.Controllers;
using LabFlow.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Task = System.Threading.Tasks.Task;

namespace LabFlow.Tests;

/// <summary>
/// Unit tests for AuthController endpoints
/// Tests registration, login, and current user endpoints
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    #region Register Endpoint Tests

    [Fact]
    public async Task Register_ValidRequest_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "doctor@hospital.com",
            Password = "SecurePass123!",
            Role = "Doctor",
            FullName = "Dr. Jane Smith"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(request.Email, request.Password, request.Role, request.FullName))
            .ReturnsAsync((true, "user123", null));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value.Should().BeOfType<RegisterResponse>().Subject;
        response.UserId.Should().Be("user123");
        response.Email.Should().Be(request.Email);
        response.Role.Should().Be(request.Role);
        response.Message.Should().Be("User registered successfully");
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "SecurePass123!",
            Role = "Doctor"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);

        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Email is required");
    }

    [Fact]
    public async Task Register_MissingPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "doctor@hospital.com",
            Password = "",
            Role = "Doctor"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Password is required");
    }

    [Fact]
    public async Task Register_MissingRole_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "doctor@hospital.com",
            Password = "SecurePass123!",
            Role = ""
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Role is required");
    }

    [Fact]
    public async Task Register_ServiceReturnsFailure_Returns400BadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "duplicate@hospital.com",
            Password = "SecurePass123!",
            Role = "Doctor"
        };

        _authServiceMock
            .Setup(s => s.RegisterAsync(request.Email, request.Password, request.Role, null))
            .ReturnsAsync((false, null, "Email already exists"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Email already exists");
    }

    #endregion

    #region Login Endpoint Tests

    [Fact]
    public async Task Login_ValidCredentials_Returns200OkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "doctor@hospital.com",
            Password = "SecurePass123!"
        };

        var expectedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test.token";

        _authServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync((true, expectedToken, 3600, null));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().Be(expectedToken);
        response.TokenType.Should().Be("Bearer");
        response.ExpiresIn.Should().Be(3600);
        response.Message.Should().Be("Login successful");
    }

    [Fact]
    public async Task Login_MissingEmail_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "",
            Password = "SecurePass123!"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Email is required");
    }

    [Fact]
    public async Task Login_MissingPassword_Returns400BadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "doctor@hospital.com",
            Password = ""
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var errorResponse = badRequestResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Password is required");
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "doctor@hospital.com",
            Password = "WrongPassword123!"
        };

        _authServiceMock
            .Setup(s => s.LoginAsync(request.Email, request.Password))
            .ReturnsAsync((false, null, null, "Invalid email or password"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.StatusCode.Should().Be(401);

        var errorResponse = unauthorizedResult.Value.Should().BeOfType<ErrorResponse>().Subject;
        errorResponse.Error.Should().Be("Invalid email or password");
    }

    #endregion

    #region GetCurrentUser Endpoint Tests

    [Fact]
    public void GetCurrentUser_AuthenticatedUser_Returns200OkWithUserInfo()
    {
        // Arrange - Setup mock user claims (as if JWT was validated)
        var claims = new[]
        {
            new Claim("sub", "user123"),
            new Claim("email", "doctor@hospital.com"),
            new Claim(ClaimTypes.Role, "Doctor"),
            new Claim(ClaimTypes.Name, "Dr. Jane Smith")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = principal
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var response = okResult.Value.Should().BeOfType<UserInfoResponse>().Subject;
        response.UserId.Should().Be("user123");
        response.Email.Should().Be("doctor@hospital.com");
        response.Role.Should().Be("Doctor");
        response.FullName.Should().Be("Dr. Jane Smith");
    }

    [Fact]
    public void GetCurrentUser_AlternativeClaimNames_Returns200Ok()
    {
        // Arrange - JWT might use different claim names
        var claims = new[]
        {
            new Claim("sub", "user456"),
            new Claim("email", "tech@hospital.com"),
            new Claim("role", "LabTechnician")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext.HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
        {
            User = principal
        };

        // Act
        var result = _controller.GetCurrentUser();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<UserInfoResponse>().Subject;
        response.UserId.Should().Be("user456");
        response.Email.Should().Be("tech@hospital.com");
        response.Role.Should().Be("LabTechnician");
    }

    #endregion
}
