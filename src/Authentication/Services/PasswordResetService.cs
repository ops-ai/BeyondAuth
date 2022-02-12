using Identity.Core;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Authentication.Services
{
    public class PasswordResetService : BackgroundService
    {
        private readonly IDocumentStore _store;

        public PasswordResetService(IDocumentStore store)
        {
            _store = store;

        }

        private Task SendPassword(string userId)
        {


            return Task.FromResult(0);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _store.Changes().ForDocumentsInCollection<PasswordResetRequest>()
                .Subscribe(change => {
                    SendPassword(change.Id).ConfigureAwait(false).GetAwaiter().GetResult();
                });

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }
        }
    }
}
