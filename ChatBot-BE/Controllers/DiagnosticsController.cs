using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatBot_BE.Data;

namespace ChatBot_BE.Controllers
{
    [ApiController]
    [Route("api/diagnostics")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public DiagnosticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                var tables = new List<string>();
                using (var command = _db.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";
                    await _db.Database.OpenConnectionAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                }
                return Ok(new { Success = true, Tables = tables });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Error = ex.Message });
            }
        }
    }
}
