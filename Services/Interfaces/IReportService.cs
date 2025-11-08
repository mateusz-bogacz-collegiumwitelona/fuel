using DTO.Requests;
using DTO.Responses;
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
        Task<Result<PagedResult<UserReportsRespnse>>> GetUserReportAsync(string email, GetPaggedRequest pagged);
    }
}
