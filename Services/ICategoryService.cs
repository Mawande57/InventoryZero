using InventoryZeroAPI.DTOs.Categories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryZeroAPI.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
    }
}