using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/brad")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class BrandContloller : ControllerBase
    {
        private readonly IBrandServices _brandServices;

        public BrandContloller(IBrandServices brandServices)
        {
            _brandServices = brandServices;
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetBrandListAsync([FromQuery] GetPaggedRequest pagged, [FromQuery] TableRequest request)
        {
            var result = await _brandServices.GetBrandToListAsync(pagged, request);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors,
                    Data = result.Data
                });
        }
    }
}
