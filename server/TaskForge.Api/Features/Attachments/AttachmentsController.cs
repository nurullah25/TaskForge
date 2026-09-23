using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Attachments;

[ApiController]
[Route("api")]
public class AttachmentsController(AttachmentService attachments) : ControllerBase
{
    [HttpGet("tasks/{taskId:int}/attachments")]
    public Task<List<AttachmentDto>> GetForTask(int taskId) => attachments.GetForTaskAsync(taskId);

    [HttpPost("tasks/{taskId:int}/attachments")]
    [RequestSizeLimit(AttachmentService.MaxFileSize + 1024)]
    public async Task<ActionResult<AttachmentDto>> Upload(int taskId, IFormFile file, CancellationToken cancellationToken)
    {
        var attachment = await attachments.UploadAsync(taskId, file, cancellationToken);
        return Created($"/api/attachments/{attachment.Id}/download", attachment);
    }

    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await attachments.DownloadAsync(id);
        return File(file.Content, file.ContentType, file.FileName);
    }

    [HttpDelete("attachments/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await attachments.DeleteAsync(id);
        return NoContent();
    }
}
