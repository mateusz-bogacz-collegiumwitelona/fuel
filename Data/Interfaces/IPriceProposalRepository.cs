using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Http;

namespace Data.Interfaces
{
    public interface IPriceProposalRepository
    {
        Task<bool> AddNewPriceProposalAsync(
            ApplicationUser user,
            Station station,
            FuelType fuelType,
            decimal priceProposal,
            IFormFile photo,
            string extension);
        Task<GetPriceProposalResponse> GetPriceProposal(string photoToken);
        Task<List<GetStationPriceProposalResponse>> GetAllPriceProposal(TableRequest request);
        Task<bool> ChangePriceProposalStatus(bool isAccepted, PriceProposal priceProposal, ApplicationUser admin);
        Task<PriceProposal> FindPriceProposal(string token);
        Task<GetPriceProposalStaisticResponse> GetPriceProposalStaisticAsync();
    }
}
