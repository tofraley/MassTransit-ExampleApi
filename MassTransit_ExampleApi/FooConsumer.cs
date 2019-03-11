using MassTransit;
using Serilog;
using Serilog.Context;
using SettlementApiMiddleware.Core.ExchangeId;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MassTransit_ExampleApi
{
    public interface IFoo
    {
        string Value { get; }
    }
    public class FooConsumer : IConsumer<IFoo>
    {
        private IServiceProvider _provider { get; set; }
        private SimpleTransient consumerTransient { get; set; }
        private SimpleScoped consumerScoped { get; set; }
        private IExchangeIdHelper consumerExchangeIdHelper { get; set; }
        private SimpleHttpProxy consumerProxy { get; set; }

        public FooConsumer(IServiceProvider provider, SimpleTransient transient, SimpleScoped scoped, IExchangeIdHelper exchangeIdHelper, SimpleHttpProxy proxy)
        {
            _provider = provider;
            consumerTransient = transient;
            consumerScoped = scoped;
            consumerExchangeIdHelper = exchangeIdHelper;
            consumerProxy = proxy;
        }

        public async Task Consume(ConsumeContext<IFoo> context)
        {
            string exchangeId = context.Headers.Get<string>(ExchangeIdHelper.ExchangeHeaderId);
            
            if (!String.IsNullOrEmpty(exchangeId))
            {
                consumerExchangeIdHelper.SetExchangeIdValue(exchangeId);
                consumerTransient.ExchangeId = exchangeId;
                consumerScoped.ExchangeId = exchangeId;
            }

            using (LogContext.PushProperty(ExchangeIdHelper.ExchangeHeaderId, exchangeId))
            {
                Log.Information("Foo Value entered: " + context.Message.Value);

                consumerProxy.GetValue(context.Message.Value);
            }
        }
    }
}
