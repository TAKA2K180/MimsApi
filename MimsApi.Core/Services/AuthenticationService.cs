using System;
using MimsApi.Core.models;

namespace MimsApi.Core.services
{
    /// <summary>
    /// Authentication response containing JWT token and user information
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public Users User { get; set; }
    }

    /// <summary>
    /// User login request model
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// User registration request model
    /// </summary>
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
