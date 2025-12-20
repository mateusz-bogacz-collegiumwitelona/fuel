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
        Task<Result<GetStationListResponse>> GetStationProfileAsync(FindStationRequest request);
        Task<Result<PagedResult<GetStationListForAdminResponse>>> GetStationsListForAdminAsync(GetPaggedRequest pagged, TableRequest request);
        Task<Result<bool>> EditStationAsync(EditStationRequest request);
        Task<Result<GetStationInfoForEditResponse>> GetStationInfoForEdit(FindStationRequest request);
        Task<Result<bool>> AddNewStationAsync(AddStationRequest request);
        Task<Result<bool>> DeleteStationAsync(FindStationRequest request);
        Task<Result<PagedResult<GetPriceProposalByStationResponse>>> GetPriceProposalByStationAsync(FindStationRequest request, GetPaggedRequest pagged);
        Task<Result<object>> GetFuelPriceHistoryAsync(FindStationRequest findStation, string? fuelCode);
    }
}
