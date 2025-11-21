using Bookify.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace Bookify.Data.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await context.Database.EnsureCreatedAsync();

            // Check if we already have data
            if (context.RoomTypes.Any())
            {
                return; // DB has been seeded
            }

            await CreateRoles(roleManager);
            await CreateAdminUser(userManager);
            await CreateRoomTypes(context);
            await CreateRooms(context);
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Customer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateAdminUser(UserManager<ApplicationUser> userManager)
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@bookify.com",
                Email = "admin@bookify.com",
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true
            };

            var user = await userManager.FindByEmailAsync(adminUser.Email);
            if (user == null)
            {
                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }

        private static async Task CreateRoomTypes(ApplicationDbContext context)
        {
            var roomTypes = new RoomType[]
            {
                new RoomType
                {
                    Name = "Single Room",
                    Description = "Cozy single room with a comfortable bed, perfect for solo travelers.",
                    Price = 99.99m,
                    MaxGuests = 1
                },
                new RoomType
                {
                    Name = "Double Room",
                    Description = "Spacious double room with two queen beds, ideal for couples or small families.",
                    Price = 149.99m,
                    MaxGuests = 2
                },
                new RoomType
                {
                    Name = "Deluxe Suite",
                    Description = "Luxury suite with separate living area, premium amenities, and stunning views.",
                    Price = 299.99m,
                    MaxGuests = 4
                },
                new RoomType
                {
                    Name = "Executive Suite",
                    Description = "Premium executive suite with workspace, luxury bathroom, and exclusive access.",
                    Price = 399.99m,
                    MaxGuests = 2
                },
                new RoomType
                {
                    Name = "Family Room",
                    Description = "Large family room with multiple beds and extra space for children.",
                    Price = 249.99m,
                    MaxGuests = 6
                }
            };

            await context.RoomTypes.AddRangeAsync(roomTypes);
            await context.SaveChangesAsync();
        }

        private static async Task CreateRooms(ApplicationDbContext context)
        {
            var roomTypes = context.RoomTypes.ToList();

            var rooms = new List<Room>();

            // Single Rooms (101-110)
            for (int i = 101; i <= 110; i++)
            {
                rooms.Add(new Room
                {
                    RoomNumber = i.ToString(),
                    RoomTypeId = roomTypes[0].Id,
                    IsAvailable = true
                });
            }

            // Double Rooms (201-210)
            for (int i = 201; i <= 210; i++)
            {
                rooms.Add(new Room
                {
                    RoomNumber = i.ToString(),
                    RoomTypeId = roomTypes[1].Id,
                    IsAvailable = i % 3 != 0 // Make some rooms unavailable for testing
                });
            }

            // Deluxe Suites (301-305)
            for (int i = 301; i <= 305; i++)
            {
                rooms.Add(new Room
                {
                    RoomNumber = i.ToString(),
                    RoomTypeId = roomTypes[2].Id,
                    IsAvailable = true
                });
            }

            // Executive Suites (401-403)
            for (int i = 401; i <= 403; i++)
            {
                rooms.Add(new Room
                {
                    RoomNumber = i.ToString(),
                    RoomTypeId = roomTypes[3].Id,
                    IsAvailable = i != 402 // Room 402 is unavailable
                });
            }

            // Family Rooms (501-504)
            for (int i = 501; i <= 504; i++)
            {
                rooms.Add(new Room
                {
                    RoomNumber = i.ToString(),
                    RoomTypeId = roomTypes[4].Id,
                    IsAvailable = true
                });
            }

            await context.Rooms.AddRangeAsync(rooms);
            await context.SaveChangesAsync();
        }
    }
}