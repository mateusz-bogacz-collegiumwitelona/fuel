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
        /// Get all stations for pin and dialog in map
        /// </summary>
        /// <returns>
        /// List of stations:
        /// - BrandName – Station brand name  
        /// - Address – Station address  
        /// - Latitude – Station latitude (Y)  
        /// - Longitude – Station longitude (X)
        /// </returns>
        /// <response code="200">Everything is fine</response>
        /// <response code="404">Can't find stations</response>
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
    }
}
