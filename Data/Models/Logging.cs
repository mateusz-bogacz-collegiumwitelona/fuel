using Data.Enums;

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
