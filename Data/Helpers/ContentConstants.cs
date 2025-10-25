using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Helpers
{
    public static class ContentConstants
    {
        public static readonly string[] FILE_TYPE_CONST = new string[]
        {
            ".jpeg", 
            ".jpg", 
            ".png", 
            ".webp"
        };

        public const long FILE_SIZE_CONST = 5 * 1024 * 1024; // 5 MB
    }
}
