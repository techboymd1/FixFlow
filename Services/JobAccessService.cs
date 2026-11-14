using System.Security.Claims;
using FixFlow.Data;
using FixFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Services;

public class JobAccessService(FixFlowDbContext database)
{
    public int GetCurrentUserId(ClaimsPrincipal user)
    {
        return int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
            ? userId
            : throw new InvalidOperationException("The signed-in user is missing an identifier.");
    }

    public async Task<bool> CanAccessJobAsync(ClaimsPrincipal user, RepairJob job, CancellationToken cancellationToken = default)
    {
        if (user.IsInRole(UserRoles.OwnerAdmin) || user.IsInRole(UserRoles.FrontDesk))
        {
            return true;
        }

        if (!user.IsInRole(UserRoles.Technician))
        {
            return false;
        }

        return job.TechnicianId == GetCurrentUserId(user);
    }

    public async Task<bool> CanAccessJobAsync(ClaimsPrincipal user, int jobId, CancellationToken cancellationToken = default)
    {
        var job = await database.RepairJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == jobId, cancellationToken);
        return job is not null && await CanAccessJobAsync(user, job, cancellationToken);
    }
}
