using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
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
        Task<Result<PagedResult<UserReportsResponse>>> GetUserReportAsync(string email, GetPaggedRequest pagged);
        Task<Result<IdentityResult>> ChangeReportStatusAsync(string adminEmail, ChangeReportStatusRequest request);
    }
}
