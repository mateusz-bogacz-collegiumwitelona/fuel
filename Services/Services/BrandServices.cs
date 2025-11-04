using Data.Interfaces;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class BrandServices : IBrandServices
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<BrandServices> _logger;

        public BrandServices(
            IBrandRepository brandRepository,
            ILogger<BrandServices> logger
            )
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetBrandDataResponse>>> GetBrandToListAsync(GetPaggedRequest pagged, TableRequest request)
        {
            try
            {
                var result = await _brandRepository.GetBrandToListAsync(request);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No brands found in the database.");

                    var emptyPaged = new PagedResult<GetBrandDataResponse>
                    {
                        Items = new List<GetBrandDataResponse>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<GetBrandDataResponse>>.Good(
                        "No brands found in the database",
                        StatusCodes.Status200OK,
                        emptyPaged
                    );
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<GetBrandDataResponse>>.Good(
                    "Brands retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving brands.");
                return Result<PagedResult<GetBrandDataResponse>>.Bad(
                    "An error occurred while retrieving brands",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
