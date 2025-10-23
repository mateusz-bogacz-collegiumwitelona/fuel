﻿using DTO.Requests;
using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IStationServices
    {
        Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync(GetStationsRequest request);
        Task<Result<List<GetStationsResponse>>> GetNearestStationAsync(double latitude, double longitude,int? count);
        Task<Result<PagedResult<GetStationListResponse>>> GetStationListAsync(GetStationListRequest request);
        Task<Result<List<string>>> GetAllBrandsAsync();
        Task<Result<GetStationListResponse>> GetStationProfileAsync(GetStationProfileRequest request);
    }
}
