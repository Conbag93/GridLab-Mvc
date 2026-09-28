using GridLab.Shared.Data;
using GridLab.Shared.Models;
using Microsoft.Extensions.DependencyInjection;

namespace GridLab.Shared;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers seeded in-memory repositories for every shared model.</summary>
    public static IServiceCollection AddGridLabData(this IServiceCollection services) => services
        .AddSingleton<IRepository<Category>>(new InMemoryRepository<Category>(SeedData.Categories()))
        .AddSingleton<IRepository<Product>>(new InMemoryRepository<Product>(SeedData.Products()))
        .AddSingleton<IRepository<User>>(new InMemoryRepository<User>(SeedData.Users()))
        .AddSingleton<IRepository<Address>>(new InMemoryRepository<Address>(SeedData.Addresses()))
        .AddSingleton<UserDirectory>();
}
