using Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Models
{
    public  class Logging
    {
        public Guid Id { get; set; }
        public string Message { get; set; }
        public string? StackTrace { get; set; }
        public string? Source { get; set; }
        public TypeOfLog TypeOfLog { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
