using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class DevelopmentDataSeeder(
    FixFlowDbContext database,
    PasswordService passwordService,
    ILogger<DevelopmentDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await database.Database.CanConnectAsync(cancellationToken))
            {
                return;
            }

            if (await database.Users.AnyAsync(cancellationToken))
            {
                return;
            }

            database.Users.AddRange(
                new AppUser
                {
                    FullName = "FixFlow Owner",
                    UserName = "owner",
                    PasswordHash = passwordService.HashPassword("ChangeMe123!"),
                    Role = UserRoles.OwnerAdmin
                },
                new AppUser
                {
                    FullName = "Front Desk User",
                    UserName = "frontdesk",
                    PasswordHash = passwordService.HashPassword("ChangeMe123!"),
                    Role = UserRoles.FrontDesk
                },
                new AppUser
                {
                    FullName = "Workshop Technician",
                    UserName = "technician",
                    PasswordHash = passwordService.HashPassword("ChangeMe123!"),
                    Role = UserRoles.Technician
                });
            await database.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Development-only FixFlow accounts were seeded. Change or remove them before production use.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "FixFlow development seed data was skipped because the database is unavailable.");
        }
    }
}
