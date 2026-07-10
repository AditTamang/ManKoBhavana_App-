using System.Security.Claims;
using ManKoBhavna.API.Data;
using ManKoBhavna.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManKoBhavna.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class JournalController : ControllerBase
    {
        private readonly AppDbContext _db;

        public JournalController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetEntries()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entries = await _db.JournalEntries
                .Where(e => e.UserId == userId.Value)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return Ok(entries);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEntry(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entry = await _db.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

            if (entry == null) return NotFound();

            return Ok(entry);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEntry([FromBody] JournalEntry entry)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            entry.UserId = userId.Value;
            entry.SavedDate = DateTime.UtcNow;
            
            // Ensure EntryDate is UTC for PostgreSQL
            if (entry.EntryDate.Kind == DateTimeKind.Unspecified || entry.EntryDate.Kind == DateTimeKind.Local)
            {
                entry.EntryDate = DateTime.SpecifyKind(entry.EntryDate, DateTimeKind.Utc);
            }

            _db.JournalEntries.Add(entry);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, entry);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntry(int id, [FromBody] JournalEntry updatedEntry)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entry = await _db.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

            if (entry == null) return NotFound();

            entry.Title = updatedEntry.Title;
            entry.Category = updatedEntry.Category;
            entry.Content = updatedEntry.Content;
            entry.Mood = updatedEntry.Mood;
            entry.Tags = updatedEntry.Tags;
            
            // Ensure EntryDate is UTC for PostgreSQL
            var newEntryDate = updatedEntry.EntryDate;
            if (newEntryDate.Kind == DateTimeKind.Unspecified || newEntryDate.Kind == DateTimeKind.Local)
            {
                newEntryDate = DateTime.SpecifyKind(newEntryDate, DateTimeKind.Utc);
            }
            entry.EntryDate = newEntryDate;
            
            entry.SavedDate = DateTime.UtcNow;
            entry.WordCount = updatedEntry.WordCount;
            entry.CharacterCount = updatedEntry.CharacterCount;

            _db.JournalEntries.Update(entry);
            await _db.SaveChangesAsync();

            return Ok(entry);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var entry = await _db.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId.Value);

            if (entry == null) return NotFound();

            _db.JournalEntries.Remove(entry);
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Entry deleted successfully." });
        }

        private int? GetCurrentUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return null;
            }
            return userId;
        }
    }
}
