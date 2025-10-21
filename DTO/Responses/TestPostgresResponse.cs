using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Responses
{
    public class TestPostgresResponse
    {
        public int Status { get; set; }
        public string? Message { get; set; }
        public string? ResponseTime { get; set; }
        public bool CanConnect { get; set; }
        public bool PostgisInstalled { get; set; }
        public string? PostgisVersion { get; set; }
    }
}
