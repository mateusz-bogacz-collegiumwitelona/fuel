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
        /// <response code="409">Conflict</response>
        /// <response code="500">Server error during update</response>

        [HttpPatch("edit/{oldName}")]
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
        /// <summary>
        /// Add a new fuel brand.
        /// </summary>
        /// <remarks>
        /// Description: Creates a new fuel brand entry in the database.  
        /// The request must contain a brand name, which will be validated to ensure it’s not null or empty.
        ///
        /// Example request (JSON)
        /// ```http
        /// POST /api/admin/brand/add
        /// Content-Type: application/json
        ///
        /// {
        ///   "name": "Orlen"
        /// }
        /// ```
        ///
        /// Example request (form-data)
        /// ```http
        /// POST /api/admin/brand/add
        /// Content-Type: multipart/form-data
        ///
        /// name=Orlen
        /// ```
        ///
        /// Example response (success)
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Brand Orlen add successful",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response (validation error)
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": [ "Name is null, empty or whitespace" ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Notes:
        /// - The brand name must be unique (case-insensitive).
        /// - The `CreatedAt` and `UpdatedAt` timestamps are automatically assigned by the server.
        /// - Only users with the **Admin** role can access this endpoint.
        /// </remarks>
        /// <param name="name">The name of the brand to add.</param>
        /// <response code="201">Brand successfully added.</response>
        /// <response code="400">Validation error – brand name is invalid or empty.</response>
        /// <response code="401">Unauthorized – missing or invalid JWT token.</response>
        /// <response code="403">Forbidden – user does not have permission to add brands.</response>
        /// <response code="409">Conflict</response>
        /// <response code="500">Internal server error while processing the request.</response>
        [HttpPost("add")]
        public async Task<IActionResult> AddBrandAsync([FromForm] string name)
        {
            var result = await _brandServices.AddBrandAsync(name);
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

        /// <summary>
        /// Delete a fuel station brand by its name.
        /// </summary>
        /// <remarks>
        /// Description:  
        /// Permanently removes a fuel station brand from the system.  
        /// Deleting a brand will also delete all associated **stations** and their related data (fuel prices, proposals, etc.),  
        /// due to cascade delete rules in the database.
        ///
        /// Example request  
        /// ```http
        /// DELETE /api/admin/brand/Orlen
        /// ```
        ///
        /// Example response — Successful deletion  
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Brand Orlen delete successfull",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response — Brand not found  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Appliaction Error",
        ///   "errors": ["Brand not found"],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response — Validation error  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Valiadtion error",
        ///   "errors": ["Name is null, empyt white space"],
        ///   "data": false
        /// }
        /// ```
        ///]
        /// Notes  
        /// - This operation is **irreversible**.  
        /// - Make sure the brand name is spelled exactly as stored in the system.  
        /// - Cascade deletion will remove all related stations and fuel data.
        ///
        /// </remarks>
        /// <param name="name">Name of the brand to delete.</param>
        /// <response code="200">Brand deleted successfully.</response>
        /// <response code="400">Validation error — brand name was empty or invalid.</response>
        /// <response code="404">Brand not found in the system.</response>
        /// <response code="500">Unexpected server error occurred while deleting the brand.</response>
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteBrandAsync(string name)
        {
            var result = await _brandServices.DeleteBrandAsync(name);
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
