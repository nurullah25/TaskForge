using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Labels;

[ApiController]
[Route("api")]
public class LabelsController(LabelService labels) : ControllerBase
{
    [HttpGet("projects/{projectId:int}/labels")]
    public Task<List<LabelDto>> GetForProject(int projectId) => labels.GetForProjectAsync(projectId);

    [HttpPost("projects/{projectId:int}/labels")]
    public async Task<ActionResult<LabelDto>> Create(int projectId, SaveLabelRequest request)
    {
        var label = await labels.CreateAsync(projectId, request);
        return Created($"/api/labels/{label.Id}", label);
    }

    [HttpPut("labels/{id:int}")]
    public Task<LabelDto> Update(int id, SaveLabelRequest request) => labels.UpdateAsync(id, request);

    [HttpDelete("labels/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await labels.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("tasks/{taskId:int}/labels")]
    public Task<List<LabelDto>> SetTaskLabels(int taskId, SetTaskLabelsRequest request) =>
        labels.SetTaskLabelsAsync(taskId, request);
}
