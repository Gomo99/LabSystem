using LaboratoryTestRequestManagementSystem.AppStatus;
using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class SampleType
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
    }
}