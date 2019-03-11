using Serilog;
using SettlementApiMiddleware.Core.ExchangeId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MassTransit_ExampleApi
{
    public class SimpleHttpProxy
    {
        private IExchangeIdHelper proxyExchangeIdHelper;
        private SimpleTransient proxyTransient;
        private SimpleScoped proxyScoped;

        public SimpleHttpProxy(IExchangeIdHelper exchangeIdHelper, SimpleTransient simpleTransient, SimpleScoped simpleScoped)
        {
            proxyExchangeIdHelper = exchangeIdHelper;
            proxyTransient = simpleTransient;
            proxyScoped = simpleScoped;
        }

        public void GetValue(string value)
        {
            Log.Information($"ExchangeIdHelper exchangeId: {proxyExchangeIdHelper.GetRequestExchangeId()}");
            Log.Information($"SimpleTransient exchangeId: {proxyTransient.ExchangeId}");
            Log.Information($"SimpleScoped exchangeId: {proxyScoped.ExchangeId}");

            var http = new HttpClient();

            var message = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5001/api/values?value={value}");
            try
            {
                message.Headers.Add(ExchangeIdHelper.ExchangeHeaderId, proxyExchangeIdHelper.GetRequestExchangeId());

            }
            catch (Exception e)
            {
                Log.Error("Error", e);
            }

            Log.Information("Sending request to simple http api...");

            var response = http.SendAsync(message);

            if (response != null)
                Log.Information($"Response recieved: {response.Result.StatusCode}");
            else
                Log.Information("There was a problem...");

        }
    }
}
