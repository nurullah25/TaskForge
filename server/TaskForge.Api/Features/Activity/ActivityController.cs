using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Activity;

[ApiController]
[Route("api")]
public class ActivityController(ActivityService activity) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/activity")]
    public Task<List<ActivityDto>> GetForTask(int taskId) => activity.GetForTaskAsync(taskId);

    [HttpGet("projects/{projectId:int}/activity")]
    public Task<PagedResult<ActivityDto>> GetForProject(int projectId, [FromQuery] PageQuery query) =>
        activity.GetForProjectAsync(projectId, query);
}
