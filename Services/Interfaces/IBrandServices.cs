using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IBrandServices
    {
        Task<Result<PagedResult<GetBrandDataResponse>>> GetBrandToListAsync(GetPaggedRequest pagged, TableRequest request);
    }
}
