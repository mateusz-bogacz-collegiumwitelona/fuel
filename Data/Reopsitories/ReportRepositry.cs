using Data.Context;
using Data.Enums;
using Data.Models;
using DTO.Responses;
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
                Status = ReportStatusEnum.Pending
            };

            await _context.ReportUserRecords.AddAsync(report);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<List<UserReportsRespnse>> GetUserReportAsync(Guid id)
            => await _context.ReportUserRecords
            .Where(ru =>
                ru.ReportedUserId == id &&
                ru.Status == ReportStatusEnum.Pending
            )
            .OrderBy(ru => ru.CreatedAt)
            .Select(ru => new UserReportsRespnse
            {
                UserName = ru.ReportedUser.UserName,
                UserEmail = ru.ReportedUser.Email,
                Reason = ru.Description,
                Staus = ru.Status.ToString(),
                CreatedAt = ru.CreatedAt
            })
            .ToListAsync();

    }
}
