using IdentityServer4.Models;
using OpenTelemetry;
using Raven.Client.Documents.Session;
using System.Diagnostics;

namespace Authentication.Infrastructure
{
    public class OpenTelemetryRavenDbExporter : BaseExporter<Activity>
    {
        private readonly IAsyncDocumentSession _session;

        public OpenTelemetryRavenDbExporter(IAsyncDocumentSession session)
        {
            _session = session;
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            using var scope = SuppressInstrumentationScope.Begin();

            foreach (var activity in batch)
            {
                if (activity.Source.Name == nameof(IdentityServerEventSink))
                {
                    if (activity.Tags.Any(s => s.Key == nameof(Client.ClientId)))
                        _session.TimeSeriesFor($"Clients/{activity.Tags.First(t => t.Key == "ClientId").Value}", activity.OperationName).Append(activity.StartTimeUtc, 1, activity.Tags.FirstOrDefault(t => t.Key == "SubjectId").Value);
                    if (activity.Tags.Any(s => s.Key == "ApiName"))
                        _session.TimeSeriesFor($"ApiResources/{activity.Tags.First(t => t.Key == "ApiName").Value}", activity.OperationName).Append(activity.StartTimeUtc, 1, activity.Tags.FirstOrDefault(t => t.Key == "SubjectId").Value);
                }
                Console.WriteLine($"RAVENDB Export: {activity.DisplayName}");
            }
            _session.SaveChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            return ExportResult.Success;
        }
    }
}
