using N4Sentinel.Domain.Exceptions;

namespace N4Sentinel.Domain.Entities;

/// <summary>
/// Signal de télémétrie JMX en temps réel (Lot 4 - FR-050) pour la mémoire Heap JVM, les threads applicatifs,
/// et les métriques des files ActiveMQ (Queue Depth, Enqueue/Dequeue Rate, KahaDB Store Percent).
/// </summary>
public class JmxMetricSignal
{
    private JmxMetricSignal()
    {
        ComponentName = string.Empty;
        MetricName = string.Empty;
        MetricValue = string.Empty;
    }

    public JmxMetricSignal(Guid environmentId, string componentName, string metricName, string metricValue, double? numericValue, string? unit)
    {
        if (string.IsNullOrWhiteSpace(componentName))
        {
            throw new DomainRuleException("Le nom du composant est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(metricName))
        {
            throw new DomainRuleException("Le nom de la métrique JMX est obligatoire.");
        }

        Id = Guid.NewGuid();
        EnvironmentId = environmentId;
        ComponentName = componentName.Trim();
        MetricName = metricName.Trim();
        MetricValue = metricValue.Trim();
        NumericValue = numericValue;
        Unit = unit?.Trim();
        CollectedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid EnvironmentId { get; private set; }

    public string ComponentName { get; private set; }

    public string MetricName { get; private set; }

    public string MetricValue { get; private set; }

    public double? NumericValue { get; private set; }

    public string? Unit { get; private set; }

    public DateTime CollectedAtUtc { get; private set; }
}
