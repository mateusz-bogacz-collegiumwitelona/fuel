using DTO.Requests;
using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IStationRepository
    {
        Task<List<GetStationsResponse>> GetAllStationsForMapAsync(GetStationsRequest request);
        Task<List<GetStationsResponse>> GetNearestStationAsync(double latitude, double longitude, int? count);
    }
}
