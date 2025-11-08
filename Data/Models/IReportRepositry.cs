using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public interface IReportRepositry
    {
        Task<bool> ReportUserAsync(ApplicationUser reported, ApplicationUser notifier, string reason);
        Task<List<UserReportsRespnse>> GetUserReportAsync(Guid id);
    }
}
