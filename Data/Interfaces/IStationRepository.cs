using Data.Models;
using DTO.Requests;
using DTO.Responses;

namespace Data.Interfaces
{
    public interface IStationRepository
    {
        Task<List<GetStationsResponse>> GetAllStationsForMapAsync(GetStationsRequest request);
        Task<List<GetStationsResponse>> GetNearestStationAsync(double latitude, double longitude, int? count);
        Task<List<GetStationListResponse>> GetStationListAsync(GetStationListRequest request);
        Task<GetStationListResponse> GetStationProfileAsync(GetStationProfileRequest request);
        Task<Station> FindStationByDataAsync(string brandName, string street, string houseNumber, string city);
        Task<List<GetStationListForAdminResponse>> GetStationsListForAdminAsync(TableRequest request);
        Task<bool> IsStationExistAsync(string brandName, string street, string houseNumber, string city);
        Task<bool> EditStationAsync(EditStationRequest request);
        Task<GetStationInfoForEditResponse> GetStationInfoForEdit(FindStationRequest request);
        Task<bool> AddNewStationAsync(AddStationRequest request);
    }
}
