using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO.Responses
{
    public class UploadPhotoResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? ResponseTime { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
