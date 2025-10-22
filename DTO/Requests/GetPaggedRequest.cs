using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Requests
{
    public class GetPaggedRequest
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
}
