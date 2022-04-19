using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using simplebibleapp.xmldatacore;

namespace simplebibleapp.Data.Bible
{
    public class XmlPathResolver : IXmlPathResolver
    {
        private readonly IWebHostEnvironment hostingEnvironment;

        public XmlPathResolver(IWebHostEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment;
        }
        public string GetPath()
        {
            var path = Path.Combine(hostingEnvironment.ContentRootPath,"Data/Bible");
            return path;
        }
    }
}
