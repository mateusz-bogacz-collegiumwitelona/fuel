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
    }
}
