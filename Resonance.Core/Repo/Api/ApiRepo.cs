using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resonance.Models;
using System.Net.Http;
using System.Net;

namespace Resonance.Repo.Api
{
    public class ApiRepo : BaseEventingRepo, IEventingRepo
    {
        //private readonly HttpMessageHandler _httpMessageHandler;
        private readonly Uri _baseAddress;

        public ApiRepo(Uri baseAddress)
        {
            if (!baseAddress.IsAbsoluteUri) throw new ArgumentException("baseAddress cannot be a relative uri", "baseAddress");
            _baseAddress = baseAddress;
        }

        //public ApiRepo(Uri baseAddress, HttpMessageHandler httpMessageHandler)
        //    : this(baseAddress)
        //{
        //    _httpMessageHandler = httpMessageHandler ?? new HttpMessageHandler();
        //}

        protected override bool ParallelQueriesSupport { get { return true; } }

        public Task<Subscription> AddOrUpdateSubscriptionAsync(Subscription subscription)
        {
            throw new NotImplementedException();
        }

        public async Task<Topic> AddOrUpdateTopicAsync(Topic topic)
        {
            using (var httpClient = CreateHttpClient())
            {
                var response = await httpClient.PostAsync("topics", topic.ToJson().ToStringContent()).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (String.IsNullOrWhiteSpace(responseContent)) return null;
                    return responseContent.FromJson<Topic>();
                }
                else
                    throw await ExceptionFor(response).ConfigureAwait(false);
            }
        }

        public async Task<Topic> GetTopicAsync(long id)
        {
            using (var httpClient = CreateHttpClient())
            {
                var response = await httpClient.GetAsync($"topics/{id}").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (String.IsNullOrWhiteSpace(responseContent)) return null;
                    return responseContent.FromJson<Topic>();
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                else
                    throw await ExceptionFor(response).ConfigureAwait(false);
            }
        }

        public async Task<Topic> GetTopicByNameAsync(string name)
        {
            using (var httpClient = CreateHttpClient())
            {
                var response = await httpClient.GetAsync($"topics/{name}").ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (String.IsNullOrWhiteSpace(responseContent)) return null;
                    return responseContent.FromJson<Topic>();
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                    return null;
                else
                    throw await ExceptionFor(response).ConfigureAwait(false);
            }
        }

        public Task<IEnumerable<Topic>> GetTopicsAsync(string partOfName = null)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTopicAsync(long id, bool inclSubscriptions)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Subscription>> GetSubscriptionsAsync(long? topicId = default(long?))
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> GetSubscriptionAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> GetSubscriptionByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSubscriptionAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<long> StorePayloadAsync(string payload)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPayloadAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeletePayloadAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ConsumableEvent>> ConsumeNextAsync(string subscriptionName, int visibilityTimeout, int maxCount = 1)
        {
            throw new NotImplementedException();
        }

        public Task MarkConsumedAsync(long id, string deliveryKey)
        {
            throw new NotImplementedException();
        }

        public Task MarkFailedAsync(long id, string deliveryKey, Reason reason)
        {
            throw new NotImplementedException();
        }

        protected override Task BeginTransactionAsync()
        {
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task CommitTransactionAsync()
        {
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }

        protected override Task RollbackTransactionAsync()
        {
#if NET452
            return Task.FromResult(0);
#else
            return Task.CompletedTask;
#endif
        }


        #region Helper methods
        /// <summary>
        /// Generic helper method to create exceptions for each HttpStatusCode
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task<Exception> ExceptionFor(HttpResponseMessage response)
        {
            if (response == null) throw new ArgumentNullException("response");
            if (response.IsSuccessStatusCode) return null;

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var exMessage = $"{response.ReasonPhrase}: {responseContent}";

            switch (response.StatusCode)
            {
                case HttpStatusCode.Gone:
                case HttpStatusCode.RequestedRangeNotSatisfiable:
                case HttpStatusCode.NotFound:
                    return new ArgumentOutOfRangeException(exMessage); // Assuming a result was expected
                case HttpStatusCode.Forbidden:
                case HttpStatusCode.ProxyAuthenticationRequired:
                case HttpStatusCode.Unauthorized:
                    return new UnauthorizedAccessException(exMessage);
                case HttpStatusCode.PreconditionFailed:
                case HttpStatusCode.BadRequest:
                    return new ArgumentException(exMessage);
                case HttpStatusCode.InternalServerError:
                    return new InvalidOperationException(exMessage);
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.RequestTimeout:
                case HttpStatusCode.ServiceUnavailable: // Not really a timeout, but usually requires same handling
                    return new TimeoutException(exMessage);
                default:
                    return new HttpRequestException(exMessage);
            }
        }

        private HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = _baseAddress;
            return httpClient;
        }
#endregion

#region IDisposable
        private bool isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                }
                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
#endregion
    }
}
