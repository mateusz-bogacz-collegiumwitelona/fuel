using DTO.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Contlollers.Controllers.Client
{
    [Route("api/station")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class StationController : ControllerBase
    {
        private readonly IStationServices _stationServices;

        public StationController(IStationServices stationServices)
        {
            _stationServices = stationServices;
        }

        /// <summary>
        /// Get all fuel stations for displaying pins and details on the map.
        /// </summary>
        /// <remarks>
        /// Description
        /// Returns a list of stations filtered by:
        /// - brand names (optional)
        /// - location and distance (optional)
        ///
        /// If no filters are provided, all stations are returned.
        ///
        /// Example request body
        /// Empty request – return all stations
        /// ```json
        /// {
        ///   "brandName": [],
        ///   "locationLatitude": null,
        ///   "locationLongitude": null,
        ///   "distance": null
        /// }
        /// ```
        ///
        /// Filter by brand and location within 10 km
        /// ```json
        /// {
        ///   "brandName": ["Orlen", "Shell"],
        ///   "locationLatitude": 52.4064,
        ///   "locationLongitude": 16.9252,
        ///   "distance": 10
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// [
        ///   {
        ///     "brandName": "Orlen",
        ///     "street": "Głogowska",
        ///     "houseNumber": "25",
        ///     "city": "Poznań",
        ///     "postalCode": "60-702",
        ///     "latitude": 52.394,
        ///     "longitude": 16.881
        ///   },
        ///   {
        ///     "brandName": "Shell",
        ///     "street": "Hetmańska",
        ///     "houseNumber": "10",
        ///     "city": "Poznań",
        ///     "postalCode": "60-251",
        ///     "latitude": 52.385,
        ///     "longitude": 16.915
        ///   }
        /// ]
        /// ```
        ///
        /// Notes
        /// - `distance` is in **kilometers**
        /// - `locationLatitude` / `locationLongitude` must be in **WGS84 format**
        ///
        /// </remarks>
        /// <response code="200">Everything is fine – stations successfully retrieved</response>
        /// <response code="404">No stations found or validation error</response>
        /// <response code="500">Something went wrong on the server (pray to the God Emperor)</response>

        [HttpPost("map/all")]
        public async Task<IActionResult> GetAllStationsForMapAsync(GetStationsRequest request)
        {
            var result = await _stationServices.GetAllStationsForMapAsync(request);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Get nearest stations. Autommatly response only 3
        /// </summary>
        /// <param name="latitude">Client latitude</param>
        /// <param name="longitude">Client longitude</param>
        /// <param name="count">How many Stations they can see</param>
        /// <remarks>
        /// Endpoint return a list of stations with their
        /// [{
        /// "brandName": string,
        /// "address": string,
        /// "latitude": double,
        ///  "longitude": double
        /// },...]
        /// </remarks>
        /// <response code="200">Everything is fine</response>
        /// <response code="404">Can't find stations / Validation error</response>
        /// <response code="500">Something bad in backend. Pray to god emperor</response>

        [HttpGet("map/nearest")]
        public async Task<IActionResult> GetNearestStationAsync(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] int? count
            )
        {
            var result = await _stationServices.GetNearestStationAsync(latitude, longitude, count);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }
    }
}
