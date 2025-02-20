using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MovieApp.Domain.Entities;
using System;
using System.Threading.Tasks;

public class SeedUsers
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            Console.WriteLine("✅ UserManager and RoleManager resolved successfully.");

            string[] roleNames = { "Admin", "Viewer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole<int> { Name = roleName, NormalizedName = roleName.ToUpper() };
                    var roleResult = await roleManager.CreateAsync(role);
                    if (roleResult.Succeeded)
                    {
                        Console.WriteLine($"✅ Role '{roleName}' created successfully.");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to create role '{roleName}':");
                        foreach (var error in roleResult.Errors)
                        {
                            Console.WriteLine($" - {error.Code}: {error.Description}");
                        }
                        throw new Exception("Role creation failed, aborting.");
                    }
                }
                else
                {
                    Console.WriteLine($"ℹ️ Role '{roleName}' already exists.");
                }
            }

            await CreateUserIfNotExists(userManager, "AdminUser", "admin@movieapp.com", "Admin@123!", "Admin", "John", "Doe");
            await CreateUserIfNotExists(userManager, "ViewerUser", "viewer@movieapp.com", "Viewer@123!", "Viewer", "Jane", "Smith");
        }
    }

    private static async Task CreateUserIfNotExists(
        UserManager<User> userManager,
        string username,
        string email,
        string password,
        string role,
        string firstName,
        string lastName)
    {
        Console.WriteLine($"Attempting to create/check user '{username}' with email '{email}'...");

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            Console.WriteLine($"User '{email}' not found, creating new user...");
            user = new User
            {
                UserName = username,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                Console.WriteLine($"✅ User '{username}' created successfully.");
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (roleResult.Succeeded)
                {
                    Console.WriteLine($"✅ User '{username}' assigned to role '{role}' successfully.");
                }
                else
                {
                    var errorMessage = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new Exception($"Failed to assign role '{role}' to user '{username}': {errorMessage}");
                }
            }
            else
            {
                var errorMessage = string.Join(", ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
                throw new Exception($"Failed to create user '{username}': {errorMessage}");
            }
        }
        else
        {
            Console.WriteLine($"ℹ️ User '{username}' already exists.");
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (roleResult.Succeeded)
                {
                    Console.WriteLine($"✅ Added existing user '{username}' to role '{role}'.");
                }
                else
                {
                    var errorMessage = string.Join(", ", roleResult.Errors.Select(e => $"{e.Code}: {e.Description}"));
                    throw new Exception($"Failed to assign role '{role}' to existing user '{username}': {errorMessage}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ User '{username}' is already in role '{role}'.");
            }
        }
    }
}