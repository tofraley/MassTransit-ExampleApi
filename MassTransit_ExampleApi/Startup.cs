using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace MassTransit_ExampleApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
                    
            var configuration = new ConfigurationBuilder().Build();
            services.AddTransient<SimpleTransient>();
            services.AddScoped<SimpleScoped>();
            services.AddSingleton<SimpleSingleton>();

            services.AddScoped<ValueEnteredConsumer>(p => new ValueEnteredConsumer(p, p.GetService<SimpleTransient>(), p.GetService<SimpleScoped>(),
                p.GetService<IExchangeIdHelper>(), p.GetService<SimpleHttpProxy>()));

            services.AddScoped<FooConsumer>(p => new FooConsumer(p, p.GetService<SimpleTransient>(), p.GetService<SimpleScoped>(),
                p.GetService<IExchangeIdHelper>(), p.GetService<SimpleHttpProxy>()));

            try
            {
                services.AddMassTransit(x =>
                {
                    x.AddConsumer<ValueEnteredConsumer>();
                    x.AddConsumer<FooConsumer>();
                });

                services.AddSingleton(provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var host = cfg.Host("localhost", 5672, "/", h =>
                    {
                        h.Username("guest");
                        h.Password("guest");
                    });

                    cfg.ReceiveEndpoint(host, "value_entered_queue", e =>
                    {
                        e.ConfigureConsumer<ValueEnteredConsumer>(provider);

                        EndpointConvention.Map<IValueEntered>(e.InputAddress);
                    });

                    cfg.ReceiveEndpoint(host, "foo_entered_queue", e =>
                    {
                        e.ConfigureConsumer<FooConsumer>(provider);

                        EndpointConvention.Map<IFoo>(e.InputAddress);
                    });
                }));

                services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
                services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
                services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());
                
                services.AddSingleton<IHostedService, BusService>();

                services.AddScoped<SimpleHttpProxy>(p => new SimpleHttpProxy(p.GetService<IExchangeIdHelper>(), p.GetService<SimpleTransient>(), p.GetService<SimpleScoped>()));
            }
            catch (Exception ex)
            {
                Log.Error("Errorrorroror  ", ex);
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
