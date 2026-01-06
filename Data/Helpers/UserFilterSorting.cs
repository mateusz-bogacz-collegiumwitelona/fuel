using DTO.Responses;

namespace Data.Helpers
{
    public class UserFilterSorting
    {
        public List<GetUserListResponse> ApplySorting(
             List<GetUserListResponse> list,
             string? sortBy,
             string? sortDirection,
             Dictionary<string, int> rolePriority)
        {
            if (string.IsNullOrEmpty(sortBy))
            {
                return list.OrderByDescending(u => rolePriority.GetValueOrDefault(u.Roles.ToLower(), 0))
                           .ThenBy(u => u.UserName)
                           .ToList();
            }

            bool isDescending = sortDirection?.ToLower() == "desc";

            return sortBy.ToLower() switch
            {
                "username" => Order(list, u => u.UserName, isDescending),
                "email" => Order(list, u => u.Email, isDescending),
                "roles" or "role" => Order(list, u => rolePriority.GetValueOrDefault(u.Roles.ToLower(), 0), isDescending),
                "createdat" or "created" => Order(list, u => u.CreatedAt, isDescending),
                "isbanned" or "banned" or "ban" => OrderWithSecondary(list, u => u.IsBanned, u => u.UserName, isDescending),
                "hasreport" or "report" => OrderWithSecondary(list, u => u.HasReport, u => u.UserName, isDescending),
                _ => list.OrderByDescending(u => rolePriority.GetValueOrDefault(u.Roles.ToLower(), 0))
                         .ThenBy(u => u.UserName)
                         .ToList()
            };
        }

        private List<GetUserListResponse> Order<TKey>(
            List<GetUserListResponse> list,
            Func<GetUserListResponse, TKey> keySelector,
            bool descending)
            => descending
                ? list.OrderByDescending(keySelector).ToList()
                : list.OrderBy(keySelector).ToList();


        private List<GetUserListResponse> OrderWithSecondary<TKey1, TKey2>(
            List<GetUserListResponse> list,
            Func<GetUserListResponse, TKey1> primaryKey,
            Func<GetUserListResponse, TKey2> secondaryKey,
            bool descending)
            => descending
                ? list.OrderByDescending(primaryKey).ThenBy(secondaryKey).ToList()
                : list.OrderBy(primaryKey).ThenBy(secondaryKey).ToList();
    }

}
