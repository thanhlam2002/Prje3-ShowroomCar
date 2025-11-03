namespace ShowroomCar.Domain.Constants
{
    public static class VehicleStatus
    {
        public const string InStock   = "IN_STOCK";
        public const string Allocated = "ALLOCATED";
        public const string Sold      = "SOLD";
    }

    public static class AllotmentStatus
    {
        public const string Reserved = "RESERVED";
        public const string Released = "RELEASED";
    }

    public static class InvoiceStatus
    {
        public const string Issued      = "ISSUED";
        public const string PaidPartial = "PAID_PARTIAL";
        public const string PaidFull    = "PAID_FULL";
    }

    public static class ServiceOrderStatus
    {
        public const string Planned    = "PLANNED";
        public const string InProgress = "IN_PROGRESS";
        public const string Done       = "DONE";
        public const string Cancelled  = "CANCELLED";
    }

    public static class PoStatus
    {
        public const string Approved  = "APPROVED";
        public const string Completed = "COMPLETED";
    }
}
