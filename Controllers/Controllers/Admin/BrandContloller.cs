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

        /// <summary>
        /// Retrieve a paginated, searchable, and sortable list of fuel station brands.
        /// </summary>
        /// <remarks>
        /// Returns a list of fuel station brands (e.g. Orlen, BP, Shell) with support for **search**, **sorting**, and **pagination**.  
        /// Useful for managing brand lists in the administration panel.
        ///
        /// Example request  
        /// ```http
        /// GET /api/admin/brand/list?Search=Orlen&amp;pageNumber=1&amp;pageSize=10&amp;sortBy=name&amp;sortDirection=asc
        /// ```
        ///
        /// Example response — Successful update  
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Brand Orlen (LPG) edited successfull",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response — Validation error  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": [ "NewName is null, empty or whitespace" ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response — Brand not found  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Server error",
        ///   "errors": [ "Cannot edit Brand" ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Notes  
        /// - `oldName` — required path parameter; current name of the brand.  
        /// - `newName` — required query parameter; new name to replace the old one.  
        /// - Updates the `UpdatedAt` timestamp automatically.  
        /// - Returns `true` if the brand was successfully updated.  
        /// </remarks>
        /// <response code="200">Brand successfully updated</response>
        /// <response code="400">Validation error (missing or invalid parameters)</response>
        /// <response code="404">Brand not found</response>
        /// <response code="500">Server error during update</response>

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

        /// <summary>
        /// Edit the name of an existing fuel station brand.
        /// </summary>
        /// <remarks>
        /// Description:  
        /// Updates the name of a fuel station brand based on its current name (`oldName`).  
        /// The new name must be provided as a query parameter (`newName`).  
        /// This endpoint is used for administrative updates of brand identifiers.
        ///
        /// Example request  
        /// ```http
        /// PUT /api/admin/brand/edit/Orlen%20(LPG)?newName=Orlen%20(Main%20Station)
        /// ```
        ///
        /// Example response — Successful update  
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Brand Orlen (LPG) edited successfull",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response — Validation error  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": [ "NewName is null, empty or whitespace" ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response — Brand not found  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Server error",
        ///   "errors": [ "Cannot edit Brand" ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Notes  
        /// - `oldName` — required path parameter; current name of the brand.  
        /// - `newName` — required query parameter; new name to replace the old one.  
        /// - Updates the `UpdatedAt` timestamp automatically.  
        /// - Returns `true` if the brand was successfully updated.  
        /// </remarks>
        /// <response code="200">Brand successfully updated</response>
        /// <response code="400">Validation error (missing or invalid parameters)</response>
        /// <response code="404">Brand not found</response>
        /// <response code="500">Server error during update</response>

        [HttpPut("edit/{oldName}")]
        public async Task<IActionResult> EditBrandAsync( string oldName, [FromQuery] string newName)
        {
            var result = await _brandServices.EditBrandAsync(oldName, newName);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message,
                    Data = result.Data
                })
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
