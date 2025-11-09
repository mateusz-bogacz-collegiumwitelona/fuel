using Data.Models;
using DTO.Requests;
using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IFuelTypeRepository
    {
        Task<FuelType> FindFuelTypeByCodeAsync(string fuelType);
        Task<List<string>> GetAllFuelTypeCodesAsync();
        Task<List<FindFuelRequest>> GetStaionFuelTypes(Guid stationId);
        Task<List<GetFuelTypeResponses>> GetFuelsTypeListAsync(TableRequest request);
        Task<bool> AddFuelTypeAsync(string name, string code);
        Task<bool> EditFuelTypeAsync(FuelType fuelType, string? name, string? code);
    }
}
