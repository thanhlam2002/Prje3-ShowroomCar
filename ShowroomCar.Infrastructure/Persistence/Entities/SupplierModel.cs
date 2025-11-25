using System;

namespace ShowroomCar.Infrastructure.Persistence.Entities
{
    public class SupplierModel
    {
        public int SupplierModelId { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public int ModelId { get; set; }
        public VehicleModel Model { get; set; }

        public DateTime? CreatedAt { get; set; }

        public ICollection<SupplierModel> SupplierModels { get; set; }


    }
}
