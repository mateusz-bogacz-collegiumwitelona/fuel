using System.ComponentModel.DataAnnotations;

namespace DTO.Requests
{
    public class EditFuelTypeRequest
    {
        [Required(ErrorMessage = "Old is required")]
        public string OldCode { get; set; }

        public string? NewName { get; set; }

        public string? NewCode{ get; set; }

    }
}
