using Microsoft.AspNetCore.Mvc;

namespace TaskForge.Api.Features.Boards;

[ApiController]
[Route("api")]
public class BoardsController(BoardService boards) : ControllerBase
{
    [HttpGet("projects/{projectId:int}/boards")]
    public Task<List<BoardSummaryDto>> GetForProject(int projectId) => boards.GetForProjectAsync(projectId);

    [HttpPost("projects/{projectId:int}/boards")]
    public async Task<ActionResult<BoardDto>> Create(int projectId, SaveBoardRequest request)
    {
        var board = await boards.CreateAsync(projectId, request);
        return CreatedAtAction(nameof(Get), new { id = board.Id }, board);
    }

    [HttpGet("boards/{id:int}")]
    public Task<BoardDto> Get(int id) => boards.GetAsync(id);

    [HttpPut("boards/{id:int}")]
    public Task<BoardDto> Update(int id, SaveBoardRequest request) => boards.UpdateAsync(id, request);

    [HttpDelete("boards/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await boards.DeleteAsync(id);
        return NoContent();
    }
}
