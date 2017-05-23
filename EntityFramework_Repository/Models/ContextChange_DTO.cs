namespace EntityFramework_Repository.Models
{
    internal class ContextChange_DTO
    {
        public OperationType Operation { get; set; }
        public object Entity { get; set; }
        public long InitialChangeLogID { get; set; }
        public long EndingChangeLogID { get; set; }
        public bool IsCommitted { get; set; }
    }
}
