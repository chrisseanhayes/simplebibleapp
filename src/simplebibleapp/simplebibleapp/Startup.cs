using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Lamar;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using NLog.Web.AspNetCore;
using simplebibleapp.Data;
using simplebibleapp.LinguisticEngine.Cache;
using simplebibleapp.LinguisticEngine.Services;
using simplebibleapp.Models;
using simplebibleapp.xmlbible;
using simplebibleapp.xmlbible.search;
using simplebibleapp.xmlbiblerepository;
using simplebibleapp.xmldatacore;
using simplebibleapp.xmldictionary;
using Microsoft.Extensions.Caching;
using NLog.Extensions.Logging;
using simplebibleapp.Hubs;
using simplebibleapp.Services;

namespace simplebibleapp
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSignalR();

            services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

            // In-memory cache for the synonym engine (avoids repeated Gemini CLI calls)
            services.AddMemoryCache();

            // ASP.NET Core Identity with SQLite
            var usersDbPath = Configuration.GetConnectionString("UsersDb")
                ?? $"Data Source={Path.Combine(AppContext.BaseDirectory, "users.db")}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(usersDbPath));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/Login";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;
            });

            services.AddDistributedRedisCache(o =>
            {
                o.Configuration = Configuration.GetConnectionString("Redis");
                o.InstanceName = "sba_web_app_session";
            });

            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(10);
                options.Cookie.HttpOnly = true;
            });
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddConfiguration(Configuration.GetSection("Logging"));
                loggingBuilder.AddNLog(Configuration);
            });
        }
 
        public void ConfigureContainer(ServiceRegistry registry)
        {
            registry.Scan(scan => {
                scan.Assembly("simplebibleapp.xmlbible");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IState>();
                scan.Assembly("simplebibleapp.xmlbiblerepository");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IXmlBibleSearchAction>();
                scan.Assembly("simplebibleapp.xmldictionary");
                scan.WithDefaultConventions();
                scan.AddAllTypesOf<IGreekDefinitionState>();
                scan.AddAllTypesOf<IHebrewDefinitionState>();
                scan.TheCallingAssembly();
                scan.WithDefaultConventions();
            });
            registry.For<IChapterBuilderFactory>().Add<ChapterBuilderFactory>();
            registry.For<IWordCountBuilderFactory>().Add<WordCountBuilderFactory>();
            registry.For<IVerseSearch>().Use<SqliteVerseSearch>();

            // Verse insight engine: VerseInsightCliService (inner) -> CachedVerseInsightCliService (L1 memory + L2 SQLite)
            registry.For<VerseInsightCliService>().Use<VerseInsightCliService>();
            registry.ForSingletonOf<VerseInsightCache>().Use<VerseInsightCache>();
            registry.For<IVerseInsightCliService>().Use(ctx =>
            {
                var inner  = ctx.GetInstance<VerseInsightCliService>();
                var l1     = ctx.GetInstance<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var l2     = ctx.GetInstance<VerseInsightCache>();
                var logger = ctx.GetInstance<Microsoft.Extensions.Logging.ILogger<CachedVerseInsightCliService>>();
                return new CachedVerseInsightCliService(inner, l1, l2, logger);
            }).Scoped();

            // Synonym engine: AgyCliService (inner) → CachedAgyLinguisticService (L1 memory + L2 SQLite)
            registry.For<AgyCliService>().Use<AgyCliService>();
            registry.ForSingletonOf<AgyLinguisticCache>().Use<AgyLinguisticCache>();
            registry.For<IAgyLinguisticService>().Use(ctx =>
            {
                var inner  = ctx.GetInstance<AgyCliService>();
                var l1     = ctx.GetInstance<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
                var l2     = ctx.GetInstance<AgyLinguisticCache>();
                var logger = ctx.GetInstance<Microsoft.Extensions.Logging.ILogger<CachedAgyLinguisticService>>();
                return new CachedAgyLinguisticService(inner, l1, l2, logger);
            }).Scoped();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddNLog();

            // Auto-create SQLite bible database if it does not exist
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var pathResolver = scope.ServiceProvider.GetRequiredService<IXmlPathResolver>();
                var states = scope.ServiceProvider.GetServices<IState>();
                
                var dataDir = Configuration["DataplanePath"] ?? pathResolver.GetPath();
                if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
                
                SqliteDbInitializer.EnsureDbCreated(pathResolver.GetPath(), dataDir, states);

                // Ensure the Agy linguistic cache table exists in its own DB file
                var agyCache = scope.ServiceProvider.GetRequiredService<AgyLinguisticCache>();
                agyCache.EnsureTableCreated();

                // Ensure the verse insight cache table exists
                var verseCache = scope.ServiceProvider.GetRequiredService<VerseInsightCache>();
                verseCache.EnsureTableCreated();

                // Auto-migrate the Identity database (creates users.db + tables on first run)
                var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                identityDb.Database.Migrate();
            }

            var fordwardedHeaderOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            fordwardedHeaderOptions.KnownNetworks.Clear();
            fordwardedHeaderOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(fordwardedHeaderOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }


            app.UseAuthentication();

            app.UseStaticFiles();

            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers(); // attribute-routed API controllers (e.g. NotesController)
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHub<LinguisticHub>("/linguisticHub");
            });
        }
    }
}
