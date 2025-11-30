using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IFuelTypeServices
    {
        Task<Result<List<string>>> GetAllFuelTypeCodesAsync();
        Task<Result<PagedResult<GetFuelTypeResponses>>> GetFuelsTypeListAsync(GetPaggedRequest pagged, TableRequest request);
        Task<Result<bool>> AddFuelTypeAsync(AddFuelTypeRequest request);
        Task<Result<bool>> EditFuelTypeAsync(EditFuelTypeRequest request);
        Task<Result<bool>> DeleteFuelTypeAsync(string code);
        Task<Result<bool>> AssignFuelTypeToStationAsync(AssignFuelTypeToStationRequest request);
    }
}
