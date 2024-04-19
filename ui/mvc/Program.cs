using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using PhiDeidPortal.Ui.Services;
using System.Net.Http;

namespace PhiDeidPortal.Ui
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                // Handling SameSite cookie according to https://docs.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-3.1
                options.HandleSameSiteCookieCompatibility();
            });

            // Sign-in users with the Microsoft identity platform
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(options =>
                {
                    var configuration = builder.Configuration.GetSection("AzureAd");
                    options.Instance = configuration["Instance"];
                    options.Domain = configuration["Domain"];
                    options.TenantId = configuration["TenantId"];
                    options.ClientId = configuration["ClientId"];
                    options.ClientSecret = configuration["ClientSecret"];
                    options.CallbackPath = configuration["CallbackPath"];
                });

            builder.Services.AddControllersWithViews(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                }).AddMicrosoftIdentityUI();

            var configuration = builder.Configuration.GetSection("StorageAccount");
            var storageAccountUri = configuration["Uri"];
            var blobServiceClient = new BlobServiceClient(new Uri(storageAccountUri), new DefaultAzureCredential());

            builder.Services.AddSingleton(x =>
            {
                return blobServiceClient;
            });
            
            builder.Services.AddSingleton<IBlobService, BlobService>(x =>
            {
                var blobService = new BlobService(blobServiceClient);
                return blobService;
            });

            builder.Services.AddSingleton<IAISearchService, AISearchService>(x =>
            {
                var indexQueryer = new AISearchService(builder.Configuration);
                return indexQueryer;
            });

            // Use a Singleton instance of the SocketsHttpHandler, which you can share across any HttpClient in your application
            SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();
            // Customize this value based on desired DNS refresh timer
            socketsHttpHandler.PooledConnectionLifetime = TimeSpan.FromMinutes(5);

            CosmosClientOptions cosmosClientOptions = new CosmosClientOptions()
            {
                // Pass your customized SocketHttpHandler to be used by the CosmosClient
                // Make sure `disposeHandler` is `false`
                HttpClientFactory = () => new HttpClient(socketsHttpHandler, disposeHandler: false)
            };

            var cosmosConnectionString = builder.Configuration.GetConnectionString("Cosmos");

            // Use a Singleton instance of the CosmosClient
            CosmosClient cosmosClient = new CosmosClient(cosmosConnectionString, cosmosClientOptions);

            builder.Services.AddSingleton(x =>
            { 
                return cosmosClient; 
            });

            //ICosmosService
            builder.Services.AddSingleton<ICosmosService, CosmosService>(x =>
            {
                return new CosmosService(cosmosClient, builder.Configuration);
            });

            // Add services to the container.
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            app.MapRazorPages();

            app.Run();
        }
    }
}
