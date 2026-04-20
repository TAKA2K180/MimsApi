using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MimsApi.Core.models;
using MimsApi.Core.services;
using MimsApi.Data.Context;
using MimsApi.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MimsApi.Data.Services
{
    /// <summary>
    /// Authentication service implementation for user registration and login
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly MimsDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthenticationService> _logger;
        private const int SaltSize = 16;
        private const int HashSize = 20;
        private const int Iterations = 10000;

        public AuthenticationService(MimsDbContext dbContext, ITokenService tokenService, ILogger<AuthenticationService> logger)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("User registration attempt: {Username}, {Email}", request.Username, request.Email);

                // Validate input
                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Password) ||
                    string.IsNullOrWhiteSpace(request.Email))
                {
                    _logger.LogWarning("Registration failed - missing required fields for user: {Username}", request.Username);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username, email, and password are required"
                    };
                }

                // Check if username already exists
                if (await _dbContext.Users.AnyAsync(u => u.Username == request.Username))
                {
                    _logger.LogWarning("Registration failed - username already exists: {Username}", request.Username);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Check if email already exists
                if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
                {
                    _logger.LogWarning("Registration failed - email already exists: {Email}", request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                var user = new Users
                {
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    Password = HashPassword(request.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Users.Add(user);
                await _dbContext.SaveChangesAsync();

                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User registered successfully: {Username} (ID: {UserId})", request.Username, user.Id);

                return new AuthResponse
                {
                    Success = true,
                    Message = "User registered successfully",
                    Token = token,
                    User = new Users
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FullName = user.FullName,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed with exception for user: {Username}", request.Username);
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("User login attempt: {Username}", request.Username);

                if (string.IsNullOrWhiteSpace(request.Username) || 
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    _logger.LogWarning("Login failed - missing credentials");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username and password are required"
                    };
                }

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null || !VerifyPassword(request.Password, user.Password))
                {
                    _logger.LogWarning("Login failed - invalid credentials for user: {Username}", request.Username);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username or password"
                    };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login failed - user account is inactive: {Username} (ID: {UserId})", request.Username, user.Id);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User account is inactive"
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();

                var token = _tokenService.GenerateToken(user);

                _logger.LogInformation("User logged in successfully: {Username} (ID: {UserId})", request.Username, user.Id);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    Token = token,
                    User = new Users
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        FullName = user.FullName,
                        IsActive = user.IsActive
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed with exception for user: {Username}", request.Username);
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput.Equals(hash);
        }
    }
}
