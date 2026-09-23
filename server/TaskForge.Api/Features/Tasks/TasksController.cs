using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Tasks;

[ApiController]
[Route("api")]
public class TasksController(TaskService tasks, TaskSearchService search) : ControllerBase
{
    // Filtering and paging live here so the board isn't the only way to find a task.
    [HttpGet("projects/{projectId:int}/tasks")]
    public Task<PagedResult<TaskListItemDto>> Search(int projectId, [FromQuery] TaskSearchQuery query) =>
        search.SearchAsync(projectId, query);

    [HttpGet("tasks/{id:int}")]
    public Task<TaskDetailsDto> Get(int id) => tasks.GetAsync(id);

    [HttpPost("tasks")]
    public async Task<ActionResult<TaskDetailsDto>> Create(CreateTaskRequest request)
    {
        var task = await tasks.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("tasks/{id:int}")]
    public Task<TaskDetailsDto> Update(int id, UpdateTaskRequest request) => tasks.UpdateAsync(id, request);

    // A move is an action rather than a field update: it recalculates the position,
    // records the status change and (from Phase 8) tells other viewers about it.
    [HttpPost("tasks/{id:int}/move")]
    public Task<TaskCardDto> Move(int id, MoveTaskRequest request) => tasks.MoveAsync(id, request);

    [HttpDelete("tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await tasks.DeleteAsync(id);
        return NoContent();
    }
}
