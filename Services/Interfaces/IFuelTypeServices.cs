using DTO.Requests;
using DTO.Responses;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IFuelTypeServices
    {
        Task<Result<List<string>>> GetAllFuelTypeCodesAsync();
        Task<Result<PagedResult<GetFuelTypeResponses>>> GetFuelsTypeListAsync(GetPaggedRequest pagged, TableRequest request);
        Task<Result<bool>> AddFuelTypeAsync(AddFuelTypeRequest request);
    }
}
