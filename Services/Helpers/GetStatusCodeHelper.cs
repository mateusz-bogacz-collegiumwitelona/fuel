using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public class GetStatusCodeHelper
    {
        public static int MapStatusCode(int status)
         =>  status switch
            {
                200 => StatusCodes.Status200OK,
                404 => StatusCodes.Status404NotFound,
                500 => StatusCodes.Status500InternalServerError,
                503 => StatusCodes.Status503ServiceUnavailable,
                504 => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status500InternalServerError
            };
    }
}
