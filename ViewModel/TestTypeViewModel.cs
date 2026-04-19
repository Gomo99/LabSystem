using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class TestTypeViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Test Name")]
        public string TestName { get; set; } = string.Empty;

        [Display(Name = "Category")]
        public int TestCategoryId { get; set; }

        [Display(Name = "Sample Type")]
        public int SampleTypeId { get; set; }

        [StringLength(50)]
        [Display(Name = "Units of Measurement")]
        public string? UnitsOfMeasurement { get; set; }

        [Display(Name = "Normal Range Min")]
        public decimal? NormalRangeMin { get; set; }

        [Display(Name = "Normal Range Max")]
        public decimal? NormalRangeMax { get; set; }

        [Display(Name = "Turnaround Time (minutes)")]
        public int TurnaroundTimeMinutes { get; set; }

        [Display(Name = "Consumables Used")]
        public List<int> SelectedConsumableIds { get; set; } = new List<int>();
    }
}
