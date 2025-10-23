using DTO.Requests;
using DTO.Responses;

namespace Data.Interfaces
{
    public interface IStationRepository
    {
        Task<List<GetStationsResponse>> GetAllStationsForMapAsync(GetStationsRequest request);
        Task<List<GetStationsResponse>> GetNearestStationAsync(double latitude, double longitude, int? count);
        Task<List<GetStationListResponse>> GetStationListAsync(GetStationListRequest request);
        Task<bool> FindBrandAsync(string brandName);
        Task<List<string>> GetAllBrandsAsync();
        Task<GetStationListResponse> GetStationProfileAsync(GetStationProfileRequest request);
    }
}
