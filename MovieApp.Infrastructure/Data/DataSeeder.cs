using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MovieApp.Domain.Entities;
using MovieApp.Infrastructure.Data;

namespace MovieApp.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // Ensure the database is created and migrated
            await context.Database.MigrateAsync();

            // Seed Roles
            await SeedRolesAsync(roleManager);

            // Seed Users
            await SeedUsersAsync(userManager);

            // Seed Movies with your specific data
            await SeedMoviesAsync(context);

            await context.SaveChangesAsync();
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager)
        {
            string[] roleNames = { "SuperAdmin", "Admin", "Viewer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = roleName });
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<User> userManager)
        {
            // Admin User (can perform CRUD operations)
            var adminUser = new User
            {
                UserName = "admin@movieapp.com",
                Email = "admin@movieapp.com",
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(adminUser.Email) == null)
            {
                var result = await userManager.CreateAsync(adminUser, "Admin@123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }

            // Viewer User (can only view movies)
            var viewerUser = new User
            {
                UserName = "viewer@movieapp.com",
                Email = "viewer@movieapp.com",
                FirstName = "Viewer",
                LastName = "User",
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(viewerUser.Email) == null)
            {
                var result = await userManager.CreateAsync(viewerUser, "Viewer@123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(viewerUser, "Viewer");
                }
                else
                {
                    throw new Exception($"Failed to create viewer user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        public static async Task SeedSuperAdmin(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            // First ensure the SuperAdmin role exists
            if (!await roleManager.RoleExistsAsync("SuperAdmin"))
            {
                await roleManager.CreateAsync(new IdentityRole<int>("SuperAdmin"));
            }

            // Check if super admin already exists
            var superAdminEmail = "superadmin@movieapp.com";
            var existingSuperAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (existingSuperAdmin == null)
            {
                var superAdmin = new User
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = null
                };

                var result = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to seed super admin user: {errors}");
                }
            }
        }

        private static async Task SeedMoviesAsync(ApplicationDbContext context)
        {
            if (!context.Movies.Any())
            {
                context.Movies.AddRange(
                    new Movie { Title = "The Shawshank Redemption", ReleaseDate = new DateTime(1994, 9, 23), Genre = "Drama", Price = 9.99m, Rating = "9.3", ImageUrl = "https://picsum.photos/200/300?random=1" },
                    new Movie { Title = "The Godfather", ReleaseDate = new DateTime(1972, 3, 24), Genre = "Crime", Price = 12.99m, Rating = "9.2", ImageUrl = "https://picsum.photos/200/300?random=2" },
                    new Movie { Title = "The Dark Knight", ReleaseDate = new DateTime(2008, 7, 18), Genre = "Action", Price = 14.99m, Rating = "9.0", ImageUrl = "https://picsum.photos/200/300?random=3" },
                    new Movie { Title = "Pulp Fiction", ReleaseDate = new DateTime(1994, 10, 14), Genre = "Crime", Price = 10.99m, Rating = "8.9", ImageUrl = "https://picsum.photos/200/300?random=4" },
                    new Movie { Title = "Forrest Gump", ReleaseDate = new DateTime(1994, 7, 6), Genre = "Drama", Price = 11.99m, Rating = "8.8", ImageUrl = "https://picsum.photos/200/300?random=5" },
                    new Movie { Title = "Inception", ReleaseDate = new DateTime(2010, 7, 16), Genre = "Sci-Fi", Price = 13.99m, Rating = "8.8", ImageUrl = "https://picsum.photos/200/300?random=6" },
                    new Movie { Title = "The Matrix", ReleaseDate = new DateTime(1999, 3, 31), Genre = "Sci-Fi", Price = 9.99m, Rating = "8.7", ImageUrl = "https://picsum.photos/200/300?random=7" },
                    new Movie { Title = "Good Will Hunting", ReleaseDate = new DateTime(1997, 12, 5), Genre = "Drama", Price = 8.99m, Rating = "8.3", ImageUrl = "https://picsum.photos/200/300?random=8" },
                    new Movie { Title = "Fight Club", ReleaseDate = new DateTime(1999, 10, 15), Genre = "Drama", Price = 10.99m, Rating = "8.8", ImageUrl = "https://picsum.photos/200/300?random=9" },
                    new Movie { Title = "The Lord of the Rings: The Fellowship of the Ring", ReleaseDate = new DateTime(2001, 12, 19), Genre = "Fantasy", Price = 15.99m, Rating = "8.8", ImageUrl = "https://picsum.photos/200/300?random=10" },
                    new Movie { Title = "The Lord of the Rings: The Two Towers", ReleaseDate = new DateTime(2002, 12, 18), Genre = "Fantasy", Price = 15.99m, Rating = "8.7", ImageUrl = "https://picsum.photos/200/300?random=11" },
                    new Movie { Title = "The Lord of the Rings: The Return of the King", ReleaseDate = new DateTime(2003, 12, 17), Genre = "Fantasy", Price = 15.99m, Rating = "8.9", ImageUrl = "https://picsum.photos/200/300?random=12" },
                    new Movie { Title = "Spider-Man", ReleaseDate = new DateTime(2002, 5, 3), Genre = "Action", Price = 12.99m, Rating = "7.3", ImageUrl = "https://picsum.photos/200/300?random=13" },
                    new Movie { Title = "Spider-Man 2", ReleaseDate = new DateTime(2004, 6, 30), Genre = "Action", Price = 12.99m, Rating = "7.3", ImageUrl = "https://picsum.photos/200/300?random=14" },
                    new Movie { Title = "Spider-Man 3", ReleaseDate = new DateTime(2007, 5, 4), Genre = "Action", Price = 12.99m, Rating = "6.2", ImageUrl = "https://picsum.photos/200/300?random=15" },
                    new Movie { Title = "The Avengers", ReleaseDate = new DateTime(2012, 5, 4), Genre = "Action", Price = 14.99m, Rating = "8.0", ImageUrl = "https://picsum.photos/200/300?random=16" },
                    new Movie { Title = "Avengers: Infinity War", ReleaseDate = new DateTime(2018, 4, 27), Genre = "Action", Price = 15.99m, Rating = "8.4", ImageUrl = "https://picsum.photos/200/300?random=17" },
                    new Movie { Title = "Avengers: Endgame", ReleaseDate = new DateTime(2019, 4, 26), Genre = "Action", Price = 15.99m, Rating = "8.4", ImageUrl = "https://picsum.photos/200/300?random=18" },
                    new Movie { Title = "Titanic", ReleaseDate = new DateTime(1997, 12, 19), Genre = "Romance", Price = 11.99m, Rating = "7.8", ImageUrl = "https://picsum.photos/200/300?random=19" },
                    new Movie { Title = "Jurassic Park", ReleaseDate = new DateTime(1993, 6, 11), Genre = "Sci-Fi", Price = 10.99m, Rating = "8.1", ImageUrl = "https://picsum.photos/200/300?random=20" },
                    new Movie { Title = "The Lion King", ReleaseDate = new DateTime(1994, 6, 24), Genre = "Animation", Price = 9.99m, Rating = "8.5", ImageUrl = "https://picsum.photos/200/300?random=21" },
                    new Movie { Title = "Toy Story", ReleaseDate = new DateTime(1995, 11, 22), Genre = "Animation", Price = 8.99m, Rating = "8.3", ImageUrl = "https://picsum.photos/200/300?random=22" },
                    new Movie { Title = "Finding Nemo", ReleaseDate = new DateTime(2003, 5, 30), Genre = "Animation", Price = 9.99m, Rating = "8.1", ImageUrl = "https://picsum.photos/200/300?random=23" },
                    new Movie { Title = "Harry Potter and the Philosopher's Stone", ReleaseDate = new DateTime(2001, 11, 16), Genre = "Fantasy", Price = 12.99m, Rating = "7.6", ImageUrl = "https://picsum.photos/200/300?random=24" },
                    new Movie { Title = "Harry Potter and the Chamber of Secrets", ReleaseDate = new DateTime(2002, 11, 15), Genre = "Fantasy", Price = 12.99m, Rating = "7.4", ImageUrl = "https://picsum.photos/200/300?random=25" },
                    new Movie { Title = "Star Wars: Episode IV - A New Hope", ReleaseDate = new DateTime(1977, 5, 25), Genre = "Sci-Fi", Price = 11.99m, Rating = "8.6", ImageUrl = "https://picsum.photos/200/300?random=26" },
                    new Movie { Title = "Star Wars: Episode V - The Empire Strikes Back", ReleaseDate = new DateTime(1980, 6, 20), Genre = "Sci-Fi", Price = 11.99m, Rating = "8.7", ImageUrl = "https://picsum.photos/200/300?random=27" },
                    new Movie { Title = "Star Wars: Episode VI - Return of the Jedi", ReleaseDate = new DateTime(1983, 5, 25), Genre = "Sci-Fi", Price = 11.99m, Rating = "8.3", ImageUrl = "https://picsum.photos/200/300?random=28" },
                    new Movie { Title = "The Silence of the Lambs", ReleaseDate = new DateTime(1991, 2, 14), Genre = "Thriller", Price = 9.99m, Rating = "8.6", ImageUrl = "https://picsum.photos/200/300?random=29" },
                    new Movie { Title = "Gladiator", ReleaseDate = new DateTime(2000, 5, 5), Genre = "Action", Price = 12.99m, Rating = "8.5", ImageUrl = "https://picsum.photos/200/300?random=30" }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}