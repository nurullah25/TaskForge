using Microsoft.AspNetCore.Mvc;
using TaskForge.Api.Common;

namespace TaskForge.Api.Features.Comments;

[ApiController]
[Route("api")]
public class CommentsController(CommentService comments) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/comments")]
    public Task<PagedResult<CommentDto>> GetForTask(int taskId, [FromQuery] PageQuery query) =>
        comments.GetForTaskAsync(taskId, query);

    [HttpPost("tasks/{taskId:int}/comments")]
    public async Task<ActionResult<CommentDto>> Add(int taskId, SaveCommentRequest request)
    {
        var comment = await comments.AddAsync(taskId, request);
        return Created($"/api/comments/{comment.Id}", comment);
    }

    [HttpPut("comments/{id:int}")]
    public Task<CommentDto> Update(int id, SaveCommentRequest request) => comments.UpdateAsync(id, request);

    [HttpDelete("comments/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await comments.DeleteAsync(id);
        return NoContent();
    }
}
