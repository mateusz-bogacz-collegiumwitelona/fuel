using DTO.Responses;

namespace Data.Interfaces
{
    public interface IBrandRepository
    {
        Task<List<GetBrandDataResponse>> GetBrandToListAsync(TableRequest request);
    }
}
