using Data.Models;
using DTO.Responses;

namespace Data.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<GetBrandDataResponse>> GetBrandToListAsync(TableRequest request);
        Task<List<string>> GetAllBrandsAsync();
        Task<bool> FindBrandAsync(string brandName);
        Task<bool> EditBrandAsync(string oldName, string newName);
        Task<bool> AddBrandAsync(string name);
        Task<bool> DeleteBrandAsync(string name);
        Task<Brand> GetBrandData(string brandName);
    }
}
