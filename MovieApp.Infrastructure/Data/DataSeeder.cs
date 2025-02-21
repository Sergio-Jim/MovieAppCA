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
                    new Movie
                    {
                        Title = "When Harry Met Sally",
                        ReleaseDate = DateTime.Parse("1989-2-12"),
                        Genre = "Romantic Comedy",
                        Rating = "R",
                        Price = 7.99M
                    },
                    new Movie
                    {
                        Title = "Ghostbusters",
                        ReleaseDate = DateTime.Parse("1984-3-13"),
                        Genre = "Comedy",
                        Rating = "R",
                        Price = 8.99M
                    },
                    new Movie
                    {
                        Title = "Ghostbusters 2",
                        ReleaseDate = DateTime.Parse("1986-2-23"),
                        Genre = "Comedy",
                        Rating = "R",
                        Price = 9.99M
                    },
                    new Movie
                    {
                        Title = "Rio Bravo",
                        ReleaseDate = DateTime.Parse("1959-4-15"),
                        Genre = "Western",
                        Rating = "R",
                        Price = 3.99M
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}