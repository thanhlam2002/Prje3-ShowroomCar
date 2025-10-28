namespace ShowroomCar.Infrastructure.Auditing
{
    // Cờ theo scope (AsyncLocal) để chặn đệ quy khi đang ghi audit
    public class AuditScope
    {
        private static readonly AsyncLocal<bool> _isAuditing = new();

        public bool IsAuditing
        {
            get => _isAuditing.Value;
            set => _isAuditing.Value = value;
        }
    }
}
