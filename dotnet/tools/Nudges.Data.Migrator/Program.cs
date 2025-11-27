using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nudges.Data.Migrator;

public class Program {
    public static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args)
        => Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(c => c.AddEnvironmentVariables())
        .ConfigureServices((context, services) => {
            services.AddDbContext<Users.UserDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString(DbConstants.UserDb)));
            services.AddDbContext<Products.ProductDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString(DbConstants.ProductDb)));
            services.AddDbContext<Payments.PaymentDbContext>(o =>
                o.UseNpgsql(context.Configuration.GetConnectionString(DbConstants.PaymentDb)));
        });
}


