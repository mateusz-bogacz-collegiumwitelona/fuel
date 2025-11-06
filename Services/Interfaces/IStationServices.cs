using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IStationServices
    {
        Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync(GetStationsRequest request);
        Task<Result<List<GetStationsResponse>>> GetNearestStationAsync(double latitude, double longitude,int? count);
        Task<Result<PagedResult<GetStationListResponse>>> GetStationListAsync(GetStationListRequest request);
        Task<Result<GetStationListResponse>> GetStationProfileAsync(GetStationProfileRequest request);
        Task<Result<PagedResult<GetStationListForAdminResponse>>> GetStationsListForAdminAsync(GetPaggedRequest pagged, TableRequest request);
        Task<Result<bool>> EditStationAsync(EditStationRequest request);
        Task<Result<bool>> AddNewStationAsync(AddStationRequest request);
    }
}
