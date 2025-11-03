// ShowroomCar.Infrastructure/Auditing/AuditScope.cs
namespace ShowroomCar.Infrastructure.Auditing
{
    // Trạng thái tạm để chặn vòng lặp audit trong SaveChanges
    public class AuditScope
    {
        public bool IsAuditing { get; set; }
    }
}
