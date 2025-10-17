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
        /// Get all stations for pin and dialog in map.
        /// </summary>
        /// <remarks>
        /// Returns a list of stations with their:
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
        [HttpGet("map/all")]
        public async Task<IActionResult> GetAllStationsForMapAsync()
        {
            var result = await _stationServices.GetAllStationsForMapAsync();
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
