using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class GetPaggedRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
        public int? PageNumber { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page size must be greater than 0")]
        public int? PageSize { get; set; }
    }
}
