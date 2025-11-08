using Data.Context;
using Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Reopsitories
{
    public class ReportRepositry : IReportRepositry
    {
        private readonly ApplicationDbContext _context;

        public ReportRepositry(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ReportUserAsync(
            ApplicationUser reported,
            ApplicationUser notifier,
            string reason)
        {
            var report = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportedUserId = reported.Id,
                ReportedUser = reported,
                ReportingUserId = notifier.Id,
                ReportingUser = notifier,
                Description = reason,
                CreatedAt = DateTime.UtcNow,
                Status = Data.Enums.ReportStatusEnum.Pending
            };

            await _context.ReportUserRecords.AddAsync(report);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}
