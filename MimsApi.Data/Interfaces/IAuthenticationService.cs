using System.Threading.Tasks;
using MimsApi.Core.services;

namespace MimsApi.Data.Interfaces
{
    /// <summary>
    /// Authentication service interface for user registration and login
    /// </summary>
    public interface IAuthenticationService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
