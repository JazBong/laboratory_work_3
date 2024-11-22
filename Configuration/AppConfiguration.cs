using Microsoft.Extensions.Configuration;

namespace ConsoleApp5.Configuration
{
    public class AppConfiguration
    {
        public string DALImplementation { get; set; }
        public string DefaultConnection { get; set; }
        public string ShopsFile { get; set; }
        public string ProductsFile { get; set; }

        public static AppConfiguration LoadConfiguration()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            return new AppConfiguration
            {
                DALImplementation = config["DALImplementation"],
                DefaultConnection = config.GetConnectionString("DefaultConnection"),
                ShopsFile = config["FilePaths:ShopsFile"],
                ProductsFile = config["FilePaths:ProductsFile"]
            };
        }
    }
}
