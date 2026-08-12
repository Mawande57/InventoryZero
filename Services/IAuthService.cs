using InventoryZeroAPI.DTOs.Auth;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
    }
}