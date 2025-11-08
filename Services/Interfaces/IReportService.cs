using DTO.Requests;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<Result<bool>> ReportUserAsync(string notifierEmail, ReportRequest request);
    }
}
