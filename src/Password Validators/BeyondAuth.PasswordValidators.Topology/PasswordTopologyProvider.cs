using Cryptography;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BeyondAuth.PasswordValidators.Topology
{
    public class PasswordTopologyProvider : IPasswordTopologyProvider
    {
        private readonly IAsyncDocumentSession _session;
        private readonly IOptions<PasswordTopologyValidatorOptions> _topologyValidatorOptions;

        public PasswordTopologyProvider(IAsyncDocumentSession session, IOptions<PasswordTopologyValidatorOptions> topologyValidatorOptions)
        {
            _session = session;
            _topologyValidatorOptions = topologyValidatorOptions;
        }

        private string GetTopologyHash(string password)
        {
            string output = Regex.Replace(password, "[a-z]", "a");
            output = Regex.Replace(output, "[A-Z]", "A");
            output = Regex.Replace(output, "\\d", "0");
            output = Regex.Replace(output, "[^aA0]", "$");

            return Hashing.GetStringSha256Hash(output);
        }

        public async Task<long> GetTopologyCount(string password)
        {
            var topologyHash = GetTopologyHash(password);

            var prefixList = new List<string>();
            var oldestDate = DateTime.Today.AddMonths(-_topologyValidatorOptions.Value.RollingHistoryInMonths);
            do
            {
                prefixList.Add($"{oldestDate:ddyyyy}-{topologyHash}");
                oldestDate = oldestDate.AddMonths(1);
            } while (oldestDate <= DateTime.Today);

            var topologyCounters = await _session.CountersFor(_topologyValidatorOptions.Value.TopologyDocumentName).GetAsync(prefixList);

            return topologyCounters.Select(t => t.Value ?? 0).Sum();
        }

        public Task IncrementTopologyCount(string password)
        {
            var topologyHash = GetTopologyHash(password);

            _session.CountersFor(_topologyValidatorOptions.Value.TopologyDocumentName).Increment($"{DateTime.Today:ddyyyy}-{topologyHash}");

            return Task.CompletedTask;
        }
    }
}
