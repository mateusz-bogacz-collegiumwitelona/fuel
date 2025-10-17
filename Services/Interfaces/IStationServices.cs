using DTO.Responses;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IStationServices
    {
        Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync();
        Task<Result<List<GetStationsResponse>>> GetNearestStationAsync(
            double latitude,
            double longitude,
            int? count
        );
    }
}
