using LaboratoryTestRequestManagementSystem.AppStatus;

namespace LaboratoryTestRequestManagementSystem.Models
{
    public class TestTypeConsumable
    {
        public int TestTypeId { get; set; }
        public TestType TestType { get; set; } = null!;

        public int ConsumableId { get; set; }
        public Consumable Consumable { get; set; } = null!;

        public Status Status { get; set; } = Status.Active;
    }
}