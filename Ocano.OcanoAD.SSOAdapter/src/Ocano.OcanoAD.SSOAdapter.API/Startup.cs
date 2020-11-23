using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Ocano.OcanoAD.SSOAdapter.Contracts.Models;
using Ocano.OcanoAD.SSOAdapter.Core.Providers;
using Ocano.OcanoAD.SSOAdapter.Core.Repositories;
using Ocano.OcanoAD.SSOAdapter.Core.Services;

namespace Ocano.OcanoAD.SSOAdapter.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AzureAdB2CConfiguration = Configuration
            .GetSection("AzureAdB2C")
            .Get<AzureAdB2CConfiguration>();
            ConfidentialClientApplication = BuildConfidentialClientApplication();
        }

        public IConfiguration Configuration { get; }
        private AzureAdB2CConfiguration AzureAdB2CConfiguration { get; set; }
        private IConfidentialClientApplication ConfidentialClientApplication { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(AzureAdB2CConfiguration);
            services.AddSingleton<UserRepository>();
            services.AddSingleton<TemporaryPasswordService>();
            ConfigureGraphServiceClient(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private IConfidentialClientApplication BuildConfidentialClientApplication()
            => ConfidentialClientApplicationBuilder
                .Create(AzureAdB2CConfiguration.ClientId)
                .WithTenantId(AzureAdB2CConfiguration.TenantId)
                .WithClientSecret(AzureAdB2CConfiguration.ClientSecret)
                .Build();

        private void ConfigureGraphServiceClient(IServiceCollection services)
        {
            services.AddSingleton(ConfidentialClientApplication);
            var clientCredentialsProvider = new ClientCredentialsProvider(ConfidentialClientApplication, AzureAdB2CConfiguration);
            services.AddSingleton(clientCredentialsProvider);
            var graphServiceClient = new GraphServiceClient(clientCredentialsProvider);
            services.AddSingleton(graphServiceClient);
        }
    }
}
