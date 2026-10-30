using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.DataAccess
{
    [Table("ProcessLog")]
    public class ProcessLog
    {
        public int Id { get; set; }
        public required string Operation { get; set; }
        public required string Component { get; set; }
        public required string Outcome { get; set; }
        public required string Message { get; set; }
        public string? ExceptionType { get; set; }
        public string? ExceptionDetails { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
