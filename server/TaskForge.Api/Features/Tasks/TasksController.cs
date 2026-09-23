using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Tasks;

[ApiController]
[Route("api/tasks")]
public class TasksController(TaskService tasks) : ControllerBase
{
    [HttpGet("{id:int}")]
    public Task<TaskDetailsDto> Get(int id) => tasks.GetAsync(id);

    [HttpPost]
    public async Task<ActionResult<TaskDetailsDto>> Create(CreateTaskRequest request)
    {
        var task = await tasks.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:int}")]
    public Task<TaskDetailsDto> Update(int id, UpdateTaskRequest request) => tasks.UpdateAsync(id, request);

    // A move is an action rather than a field update: it recalculates the position,
    // records the status change and (from Phase 8) tells other viewers about it.
    [HttpPost("{id:int}/move")]
    public Task<TaskCardDto> Move(int id, MoveTaskRequest request) => tasks.MoveAsync(id, request);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await tasks.DeleteAsync(id);
        return NoContent();
    }
}
