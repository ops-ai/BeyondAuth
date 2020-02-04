using Autofac;
using Flexinets.Net;
using Flexinets.Radius;
using Flexinets.Radius.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;

namespace RadiusService
{
    public class Startup
    {
        private RadiusServer _authenticationServer;
        private RadiusServer _accountingServer;

        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Startup>();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            logger.LogInformation("Reading configuration");

            var dictionary = new RadiusDictionary("radius.dictionary", loggerFactory.CreateLogger<RadiusDictionary>());
            logger.LogInformation("Configuration read");

            var radiusPacketParser = new RadiusPacketParser(loggerFactory.CreateLogger<RadiusPacketParser>(), dictionary);
            var packetHandler = new TestPacketHandler();
            var repository = new PacketHandlerRepository();
            repository.AddPacketHandler(IPAddress.Any, packetHandler, Configuration["secret"]);

            var udpClientFactory = new UdpClientFactory();

            _authenticationServer = new RadiusServer(udpClientFactory, new IPEndPoint(IPAddress.Any, 1812), radiusPacketParser, RadiusServerType.Authentication, repository, loggerFactory.CreateLogger<RadiusServer>());
            _accountingServer = new RadiusServer(udpClientFactory, new IPEndPoint(IPAddress.Any, 1813), radiusPacketParser, RadiusServerType.Accounting, repository, loggerFactory.CreateLogger<RadiusServer>());

            _authenticationServer.Start();
            _accountingServer.Start();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
