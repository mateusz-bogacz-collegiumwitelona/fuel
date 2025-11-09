using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using NRedisStack;
using Services.Interfaces;
using Services.Services;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/fuel-type")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class FuelTypeController : ControllerBase
    {
        private readonly IFuelTypeServices _fuelTypeServices;

        public FuelTypeController(
            IFuelTypeServices fuelTypeServices)
        {
            _fuelTypeServices = fuelTypeServices;
        }

        /// <summary>
        /// Retrieve a paginated, searchable, and sortable list of fuel types.
        /// </summary>
        /// <remarks>
        /// Returns a list of fuel types (e.g. PB95, PB98, ON, LPG, E85) with support for **search**, **sorting**, and **pagination**.  
        /// This endpoint is used in the administration panel to manage available fuel types.
        ///
        /// Example request  
        /// ```http
        /// GET /api/admin/fuel-type/list?pageNumber=1&amp;pageSize=10&amp;search=PB&amp;sortBy=name&amp;sortDirection=asc
        /// ```
        ///
        /// Example response — Successful retrieval  
        /// ```json
        /// {
        ///   "items": [
        ///     {
        ///       "name": "E85",
        ///       "code": "E85",
        ///       "createdAt": "2025-11-09T19:38:47.253332Z",
        ///       "updatedAt": "0001-01-01T00:00:00"
        ///     },
        ///     {
        ///       "name": "LPG",
        ///       "code": "LPG",
        ///       "createdAt": "2025-11-09T19:38:47.249874Z",
        ///       "updatedAt": "0001-01-01T00:00:00"
        ///     }
        ///   ],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 5,
        ///   "totalPages": 1,
        ///   "hasPreviousPage": false,
        ///   "hasNextPage": false
        /// }
        /// ```
        ///
        /// Example response — No results found  
        /// ```json
        /// {
        ///   "items": [],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 0,
        ///   "totalPages": 0,
        ///   "hasPreviousPage": false,
        ///   "hasNextPage": false
        /// }
        /// ```
        ///
        /// Notes  
        /// - Supports query parameters:  
        ///   - `pageNumber` *(optional, default = 1)* — specifies which page of results to return.  
        ///   - `pageSize` *(optional, default = 10)* — specifies how many items per page.  
        ///   - `search` *(optional)* — filters results by matching `name` or `code`.  
        ///   - `sortBy` *(optional)* — allows sorting by `name`, `code`, `createdAt`, or `updatedAt`.  
        ///   - `sortDirection` *(optional)* — `asc` or `desc`.  
        /// - Returns a paginated structure with metadata (total pages, total count, etc.).  
        /// </remarks>
        /// <response code="200">List of fuel types successfully retrieved</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Unauthorized — missing or invalid token</response>
        /// <response code="500">Server error while retrieving fuel types</response>
        [HttpGet("list")]
        public async Task<IActionResult> GetFuelsTypeList([FromQuery] GetPaggedRequest pagged, [FromQuery] TableRequest request)
        {
            var result = await _fuelTypeServices.GetFuelsTypeListAsync(pagged, request);

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
        /// Add a new fuel type.
        /// </summary>
        /// <remarks>
        /// Creates a new fuel type entry (e.g. **PB95**, **ON**, **LPG**, **E85**) in the system.  
        /// The request validates and automatically formats data before saving:
        /// - **Code** → uppercase without spaces (e.g. `" pb 95 "` → `"PB95"`)  
        /// - **Name** → each word capitalized (e.g. `"olej napędowy"` → `"Olej Napędowy"`)  
        ///
        /// Example request  
        /// ```http
        /// POST /api/admin/fuel-type/add
        /// Content-Type: application/json
        ///
        /// {
        ///   "name": "benzyna pb95",
        ///   "code": "pb95"
        /// }
        /// ```
        ///
        /// Example response — Successful creation  
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Fuel type added successfully.",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response — Validation error  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "ValidationError",
        ///   "errors": [ "Fuel name cannot be empty." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response — Conflict (fuel type already exists)  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Fuel type already exists.",
        ///   "errors": [ "Fuel type with code PB95 already exists." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Notes  
        /// - Requires **Admin** role authorization.  
        /// - `code` must be unique, uppercase, and alphanumeric.  
        /// - `name` is automatically formatted to title case.  
        /// - Returns `201 Created` on success.  
        /// </remarks>
        /// <param name="request">Fuel type creation payload (name and code)</param>
        /// <response code="201">Fuel type successfully created</response>
        /// <response code="400">Validation error (missing or invalid parameters)</response>
        /// <response code="409">Fuel type with the same code already exists</response>
        /// <response code="401">Unauthorized — Admin role required</response>
        /// <response code="500">Server error while adding the fuel type</response>
        [HttpPost("add")]
        public async Task<IActionResult> AddFuelTypeAsync([FromBody] AddFuelTypeRequest request)
        {
            var result = await _fuelTypeServices.AddFuelTypeAsync(request);
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
        /// Edit an existing fuel type (name and/or code).
        /// </summary>
        /// <remarks>
        /// Updates an existing fuel type identified by its <b>OldCode</b>.  
        /// You can modify one or both of the following fields: <b>NewName</b> and <b>NewCode</b>.  
        /// 
        /// The request automatically enforces formatting rules:
        /// - <b>NewCode</b> → spaces removed, converted to uppercase (e.g., "pb95" → "PB95")  
        /// - <b>NewName</b> → each word capitalized (e.g., "olej napedowy" → "Olej Napędowy")
        ///
        /// Example request:
        /// ```http
        /// PATCH /api/admin/fuel-type/edit
        /// Content-Type: application/json
        ///
        /// {
        ///   "oldCode": "PB95",
        ///   "newName": "Benzyna Premium 95",
        ///   "newCode": "PB95P"
        /// }
        /// ```
        ///
        /// Example successful response:
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Fuel type edited successfully.",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response – Validation error:
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "ValidationError",
        ///   "errors": [ "At least one of NewName or NewCode must be provided." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response – Conflict (code already exists):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Fuel type already exists.",
        ///   "errors": [ "Fuel type with code PB98 already exists." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Example response – Not found:
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Fuel type does not exist.",
        ///   "errors": [ "Fuel type with code PB90 does not exist." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// Notes:
        /// - If <b>NewCode</b> already exists, a 409 Conflict is returned.
        /// - If <b>OldCode</b> is invalid or not found, a 404 Not Found is returned.
        /// - You must provide at least one of <b>NewName</b> or <b>NewCode</b>.
        /// - The <b>UpdatedAt</b> field is refreshed automatically upon success.
        /// </remarks>
        /// <response code="200">Fuel type edited successfully</response>
        /// <response code="400">Validation error (missing or invalid fields)</response>
        /// <response code="404">Fuel type with provided OldCode not found</response>
        /// <response code="409">Conflict – new fuel code already exists</response>
        /// <response code="500">Server error during edit operation</response>
        [HttpPatch("edit")]
        public async Task<IActionResult> EditFuelTypeAsync([FromBody] EditFuelTypeRequest request)
        {
            var result = await _fuelTypeServices.EditFuelTypeAsync(request);
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
