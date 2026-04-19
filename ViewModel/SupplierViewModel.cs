using System.ComponentModel.DataAnnotations;

namespace LaboratoryTestRequestManagementSystem.ViewModel
{
    public class SupplierViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string? ContactPerson { get; set; }

        [EmailAddress]
        [Display(Name = "Email Address")]
        public string? EmailAddress { get; set; }
    }

}
