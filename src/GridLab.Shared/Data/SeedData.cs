using GridLab.Shared.Models;

namespace GridLab.Shared.Data;

public static class SeedData
{
    public static IEnumerable<Category> Categories() =>
    [
        new() { Id = 1, Name = "Hardware" },
        new() { Id = 2, Name = "Software" },
        new() { Id = 3, Name = "Accessories" },
        new() { Id = 4, Name = "Services" },
    ];

    public static IEnumerable<Product> Products() => HandWritten().Concat(Generated(60));

    private static IEnumerable<Product> HandWritten()
    {
        var now = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        return
        [
            new() { Id = 1,  Name = "Workstation Pro 14",     CategoryId = 1, UnitPrice = 1899.00m, UnitsInStock = 12,  LastUpdated = now.AddDays(-1) },
            new() { Id = 2,  Name = "Compact Desktop",        CategoryId = 1, UnitPrice = 649.00m,  UnitsInStock = 30,  LastUpdated = now.AddDays(-2) },
            new() { Id = 3,  Name = "27\" 4K Monitor",        CategoryId = 1, UnitPrice = 429.99m,  UnitsInStock = 45,  LastUpdated = now.AddDays(-3) },
            new() { Id = 4,  Name = "Grid Studio (annual)",   CategoryId = 2, UnitPrice = 299.00m,  UnitsInStock = 999, LastUpdated = now.AddDays(-4) },
            new() { Id = 5,  Name = "Report Designer",        CategoryId = 2, UnitPrice = 149.00m,  UnitsInStock = 999, LastUpdated = now.AddDays(-5) },
            new() { Id = 6,  Name = "Legacy Toolkit 2019",    CategoryId = 2, UnitPrice = 49.00m,   UnitsInStock = 0,   Discontinued = true, LastUpdated = now.AddDays(-40) },
            new() { Id = 7,  Name = "Mechanical Keyboard",    CategoryId = 3, UnitPrice = 119.00m,  UnitsInStock = 80,  LastUpdated = now.AddDays(-6) },
            new() { Id = 8,  Name = "Wireless Mouse",         CategoryId = 3, UnitPrice = 39.50m,   UnitsInStock = 150, LastUpdated = now.AddDays(-7) },
            new() { Id = 9,  Name = "USB-C Dock",             CategoryId = 3, UnitPrice = 179.00m,  UnitsInStock = 25,  LastUpdated = now.AddDays(-8) },
            new() { Id = 10, Name = "Laptop Sleeve 14\"",     CategoryId = 3, UnitPrice = 24.99m,   UnitsInStock = 0,   Discontinued = true, LastUpdated = now.AddDays(-60) },
            new() { Id = 11, Name = "Onboarding Workshop",    CategoryId = 4, UnitPrice = 1200.00m, UnitsInStock = 5,   LastUpdated = now.AddDays(-9) },
            new() { Id = 12, Name = "Priority Support (1 yr)", CategoryId = 4, UnitPrice = 850.00m, UnitsInStock = 50,  LastUpdated = now.AddDays(-10) },
        ];
    }

    /// <summary>Deterministic filler rows (fixed random seed) so the grids have enough data to page through.</summary>
    private static IEnumerable<Product> Generated(int count)
    {
        string[] adjectives = ["Compact", "Rugged", "Wireless", "Modular", "Premium", "Basic", "Smart", "Portable"];
        string[] nouns = ["Adapter", "Cable", "Charger", "Hub", "Stand", "Speaker", "Headset", "Webcam", "Sensor", "Bracket"];
        var rng = new Random(20260901);
        var start = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < count; i++)
        {
            var id = 13 + i;
            yield return new Product
            {
                Id = id,
                Name = $"{adjectives[rng.Next(adjectives.Length)]} {nouns[rng.Next(nouns.Length)]} {id}",
                CategoryId = rng.Next(1, 5),
                UnitPrice = Math.Round((decimal)(rng.NextDouble() * 400 + 5), 2),
                UnitsInStock = rng.Next(0, 300),
                Discontinued = rng.Next(10) == 0,
                LastUpdated = start.AddHours(i * 7),
            };
        }
    }

    private static readonly string[] FirstNames = ["Olivia", "Noah", "Amelia", "Liam", "Isla", "Oliver", "Ava", "George", "Mia", "Arthur", "Freya", "Leo", "Grace", "Oscar", "Lily", "Harry", "Sophia", "Jack", "Ella", "Charlie", "Aisha", "Muhammad", "Zara", "Yusuf", "Priya", "Arjun", "Chloe", "Ethan", "Hannah", "Daniel"];
    private static readonly string[] LastNames = ["Smith", "Jones", "Taylor", "Brown", "Williams", "Wilson", "Johnson", "Davies", "Robinson", "Wright", "Thompson", "Evans", "Walker", "White", "Roberts", "Green", "Hall", "Wood", "Jackson", "Clarke", "Patel", "Khan", "Lewis", "Harris", "Martin", "Cooper", "Ward", "King", "Baker", "Hughes"];

    /// <summary>A few thousand users so paging, search and sort have something to chew on.</summary>
    public static IEnumerable<User> Users(int count = 10_000)
    {
        var rng = new Random(20260916);
        var start = new DateTime(2022, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        for (var i = 1; i <= count; i++)
        {
            var first = FirstNames[rng.Next(FirstNames.Length)];
            var last = LastNames[rng.Next(LastNames.Length)];
            var created = start.AddHours(rng.Next(0, 24 * 365 * 4));
            yield return new User
            {
                Id = i,
                FirstName = first,
                LastName = last,
                Email = $"{first}.{last}{i}@example.com".ToLowerInvariant(),
                Role = rng.Next(20) == 0 ? UserRole.Admin : rng.Next(4) == 0 ? UserRole.Editor : UserRole.Viewer,
                IsActive = rng.Next(10) != 0,
                CreatedOn = created,
                LastLogin = rng.Next(5) == 0 ? null : created.AddDays(rng.Next(1, 700)),
            };
        }
    }

    public static readonly IReadOnlyList<(string Code, string Name)> Countries =
    [
        ("GB", "United Kingdom"), ("IE", "Ireland"), ("FR", "France"), ("DE", "Germany"), ("ES", "Spain"),
        ("NL", "Netherlands"), ("US", "United States"), ("CA", "Canada"), ("AU", "Australia"),
    ];

    public static IEnumerable<Address> Addresses(int count = 400, int userCount = 10_000)
    {
        var rng = new Random(20260917);
        string[] streets = ["High Street", "Station Road", "Church Lane", "Park Avenue", "Mill Road", "Queen's Way", "Victoria Terrace", "The Green"];
        string[] cities = ["London", "Manchester", "Bristol", "Leeds", "Glasgow", "Cardiff", "Belfast", "Edinburgh", "Dublin", "Birmingham"];
        for (var i = 1; i <= count; i++)
        {
            yield return new Address
            {
                Id = i,
                UserId = rng.Next(1, userCount + 1),
                Kind = (AddressKind)rng.Next(4),
                Line1 = $"{rng.Next(1, 200)} {streets[rng.Next(streets.Length)]}",
                City = cities[rng.Next(cities.Length)],
                PostCode = $"{(char)('A' + rng.Next(26))}{(char)('A' + rng.Next(26))}{rng.Next(1, 30)} {rng.Next(1, 9)}{(char)('A' + rng.Next(26))}{(char)('A' + rng.Next(26))}",
                Country = Countries[rng.Next(3) == 0 ? rng.Next(Countries.Count) : 0].Code,
                IsPrimary = rng.Next(3) == 0,
            };
        }
    }
}
