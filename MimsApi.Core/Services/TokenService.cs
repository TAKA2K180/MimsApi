using MimsApi.Core.models;

namespace MimsApi.Core.services
{
    /// <summary>
    /// Service for generating JWT tokens
    /// </summary>
    public interface ITokenService
    {
        string GenerateToken(Users user);
    }
}
