
namespace HomeDB.Domain.Exceptions
{
    public class MetricNotFoundException : Exception
    {
        public MetricNotFoundException()
            : base("Unable to find the specified metric") { }
    }
}
