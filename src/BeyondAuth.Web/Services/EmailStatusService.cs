using BeyondAuth.Web.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Commands.Batches;
using Raven.Client.Documents.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BeyondAuth.Web.Services
{
    public class EmailStatusService : BackgroundService
    {
        private IDocumentStore _store;
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _clientFactory;

        public EmailStatusService(IDocumentStore store, ILoggerFactory loggerFactory, IHttpClientFactory clientFactory)
        {
            _store = store;
            _logger = loggerFactory.CreateLogger<EmailStatusService>();
            _clientFactory = clientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var lastTimeRetrieved = DateTime.UtcNow.AddMinutes(-30);
                while (true)
                {
                    using (var httpClient = _clientFactory.CreateClient("mailgun-api"))
                    {
                        var url = "events" + $"?begin={lastTimeRetrieved.ToString("R")}&ascending=yes&limit=25&pretty=no";

                        do
                        {
                            var result = await httpClient.GetAsync(url, stoppingToken);
                            result.EnsureSuccessStatusCode();

                            var mailGunResponse = await result.Content.ReadAsAsync<EventPage>(stoppingToken);

                            if (!mailGunResponse.Items.Any())
                                await Task.Delay(15000, stoppingToken);
                            else
                            {
                                //if (UnixTimeStampToDateTime((double)mailGunResponse.Items.Last().Timestamp) > DateTime.UtcNow.AddMinutes(-30))
                                //{
                                //    await Task.Delay(15000, stoppingToken);
                                //    continue;
                                //}

                                using (var session = _store.OpenAsyncSession())
                                {
                                    foreach (var e in mailGunResponse.Items)
                                    {
                                        session.Advanced.Defer(new PatchCommandData(
                                            id: $"SentEmails/{e.Message.Headers.MessageId}",
                                            changeVector: null,
                                            patch: new PatchRequest
                                            {
                                                Script = $"if (this.Events.find(e => e.Id == '{e.Id}') == null)" +
                                                         $"this.Events.push(args.e);",
                                                Values = { { "e", new EmailEvent { Id = e.Id, Name = e.@event, CreatedOnUtc = UnixTimeStampToDateTime((double)e.Timestamp) } } }
                                            },
                                            patchIfMissing: null));
                                    }
                                    await session.SaveChangesAsync(stoppingToken);
                                }
                            }
                            url = mailGunResponse.Paging.Next;
                        }
                        while (url != null && !stoppingToken.IsCancellationRequested);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting email events");
            }
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        class EventPage
        {
            public List<Event> Items { get; set; }

            public Paging Paging { get; set; }
        }

        class Paging
        {
            public string Next { get; set; }

            public string Previous { get; set; }
        }

        class Event
        {
            public string Id { get; set; }

            public decimal Timestamp { get; set; }

            public string @event { get; set; }

            public Message Message { get; set; }
        }

        class Message
        {
            public Headers Headers { get; set; }

            public double Size { get; set; }
        }

        class Headers
        {
            [JsonProperty("message-id")]
            public string MessageId { get; set; }

            public string To { get; set; }

            public string From { get; set; }

            public string Subject { get; set; }
        }
    }
}
