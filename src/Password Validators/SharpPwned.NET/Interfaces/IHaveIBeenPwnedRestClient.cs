﻿using SharpPwned.NET.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharpPwned.NET.Interfaces
{
    public interface IHaveIBeenPwnedRestClient
    {
        Task<List<Paste>> GetPasteAccount(string account);
        Task<Breach> GetBreach(string site);
        Task<List<Breach>> GetAllBreaches();
        Task<List<Breach>> GetAccountBreaches(string account, bool? includeUnverified = true);
        Task<bool> IsPasswordPwned(string password);
    }
}