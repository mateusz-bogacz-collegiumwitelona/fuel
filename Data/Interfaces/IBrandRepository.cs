using DTO.Responses;

namespace Data.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<GetBrandDataResponse>> GetBrandToListAsync(TableRequest request);
        Task<List<string>> GetAllBrandsAsync();
        Task<bool> FindBrandAsync(string brandName);
        Task<bool> EditBrandAsync(string oldName, string newName);
    }
}
