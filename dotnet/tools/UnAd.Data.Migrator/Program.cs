using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace UnAd.Data.Migrator;

public class Program {
    public static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(c => c.AddEnvironmentVariables())
        .ConfigureServices((context, services) => {
            services.AddDbContext<Users.UserDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString("UserDb")));
            services.AddDbContext<Products.ProductDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString("ProductDb")));
            services.AddDbContext<Payments.PaymentDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString("PaymentDb")));
        });
}


