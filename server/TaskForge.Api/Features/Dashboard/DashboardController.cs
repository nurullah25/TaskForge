using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<DashboardDto> Get(int organizationId) => dashboard.GetAsync(organizationId);
}
