namespace ShowroomCar.Application.Dtos
{
    public class PurchaseOrderUpdateRequest
    {
        public int? SupplierId { get; set; }
        public DateOnly? OrderDate { get; set; }
        public List<PurchaseOrderItemDto>? Items { get; set; }
    }
}
