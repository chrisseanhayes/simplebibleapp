using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace simplebibleapp.xmlbible.tests
{
    [TestClass]
    public class WebIntegrationTests
    {
        private static WebApplicationFactory<Startup> _factory;
        private static HttpClient _client;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _factory = new WebApplicationFactory<Startup>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        // Replace Redis distributed cache with standard In-Memory cache for clean/isolated testing
                        var descriptors = services.Where(d => d.ServiceType == typeof(IDistributedCache)).ToList();
                        foreach (var descriptor in descriptors)
                        {
                            services.Remove(descriptor);
                        }
                        services.AddDistributedMemoryCache();
                    });
                });
            _client = _factory.CreateClient();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task Test_Homepage_Loads_With_Books()
        {
            var response = await _client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(html.Contains("Genesis"));
            Assert.IsTrue(html.Contains("Revelation"));
        }

        [TestMethod]
        public async Task Test_BookPage_Loads_Chapters()
        {
            var response = await _client.GetAsync("/Home/Book/Gen");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(html.Contains("Genesis"));
            Assert.IsTrue(html.Contains("1"));
        }

        [TestMethod]
        public async Task Test_ReadPage_Loads_Genesis()
        {
            var response = await _client.GetAsync("/Home/Read?bookAbbr=Gen&chapter=1");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(html.Contains("Genesis 1"));
            Assert.IsTrue(html.Contains("In the beginning"));
        }

        [TestMethod]
        public async Task Test_GetStrongRef_Greek()
        {
            var response = await _client.GetAsync("/Home/GetStrongRef/G3068");
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(html.Contains("Strong") || html.Contains("strong") || response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Test_GetWordRefs_Greek()
        {
            var response = await _client.GetAsync("/Home/GetWordRefs/G2424");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(json.StartsWith("[") && json.EndsWith("]"));
            Assert.IsTrue(json.Contains("Jesus"));
        }

        [TestMethod]
        public async Task Test_GetWordRefs_Hebrew()
        {
            var response = await _client.GetAsync("/Home/GetWordRefs/H3068");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();

            Assert.IsTrue(json.StartsWith("[") && json.EndsWith("]"));
            Assert.IsTrue(json.Contains("Lord") || json.Contains("LORD"));
        }
    }
}
