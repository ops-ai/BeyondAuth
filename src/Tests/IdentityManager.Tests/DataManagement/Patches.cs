using CsvHelper;
using Identity.Core;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.CompareExchange;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace IdentityManager.Tests.DataManagement
{
    public class Patches : IClassFixture<RavenDbFixture>
    {
        private IDocumentStore _store;

        public Patches(RavenDbFixture ravendbFixture) => _store = ravendbFixture.Store;


        //[Fact]
        public async Task UpdateChats()
        {
            using (var session = _store.OpenAsyncSession(""))
            {
                using (var reader = new StreamReader("C:\\temp\\users.csv"))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var records = csv.GetRecords<ApplicationUser>().ToList();
                    foreach (var user in records)
                    {
                        user.Organization = "DoctorPup";
                        user.ChangePasswordOnNextLogin = false;
                        user.ChangePasswordAllowed = true;
                        user.Disabled = false;
                        user.UpdatedAt = DateTime.UtcNow;
                        user.DefaultApp = "https://doctorpup.com";
                        user.DisplayName = $"{user.FirstName}";

                        await session.StoreAsync(user);
                    }

                    await session.SaveChangesAsync();

                    foreach (var user in records)
                    {
                        var put = new PutCompareExchangeValueOperation<string>($"emails/{user.Email.ToLower()}", user.Id, 0);

                        var putResult = _store.Operations.Send(put);
                    }
                }
            }
        }
    }
}
