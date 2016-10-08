using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Repo.Api
{
    public class ApiRepoFactory : IEventingRepoFactory
    {
        private readonly Uri _apiBaseAddress;

        public ApiRepoFactory(Uri apiBaseAddress)
        {
            _apiBaseAddress = apiBaseAddress;
        }

        public IEventingRepo CreateRepo()
        {
            return new ApiRepo(_apiBaseAddress);
        }
    }
}
