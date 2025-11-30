using Data.Models;
using DTO.Requests;
using DTO.Responses;

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
        Task<bool> DeleteFuelTypeAsync(FuelType fuelType);
        Task<bool> AssignFuelTypeToStationAsync(Guid fuelTypeId, Guid stationId, decimal price);
        Task<List<GetFuelPriceAndCodeResponse>> GetFuelPriceForStationAsync(FindStationRequest request);
        Task<bool> ChangeFuelPriceAsync(Guid stationId, Guid fuelTypeId, decimal price);
    }
}
