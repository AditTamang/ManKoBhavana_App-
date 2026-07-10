using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManKoBhavna.API.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        [Required]
        public DateTime EntryDate { get; set; }

        [Required]
        public string Title { get; set; } = "Untitled";

        [Required]
        public string Category { get; set; } = "General";

        [Required(ErrorMessage = "Please write something in your journal entry.")]
        [StringLength(5000, ErrorMessage = "Entry content cannot exceed 5000 characters.")]
        public string Content { get; set; } = "";

        [Required]
        public string Mood { get; set; } = "";

        [Required]
        public string Tags { get; set; } = "";

        public DateTime? SavedDate { get; set; }

        public int WordCount { get; set; }

        public int CharacterCount { get; set; }

        // Helper methods for working with multiple moods (stored as JSON array in Mood field)
        public List<string> GetMoods()
        {
            if (string.IsNullOrEmpty(Mood))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(Mood) ?? new List<string>();
            }
            catch
            {
                return string.IsNullOrEmpty(Mood) ? new List<string>() : new List<string> { Mood };
            }
        }

        public void SetMoods(List<string> moods)
        {
            if (moods == null || moods.Count == 0)
            {
                Mood = "";
                return;
            }

            Mood = JsonSerializer.Serialize(moods);
        }

        public string? GetPrimaryMood()
        {
            var moods = GetMoods();
            return moods.Count > 0 ? moods[0] : null;
        }

        public string? GetSecondaryMood()
        {
            var moods = GetMoods();
            return moods.Count > 1 ? moods[1] : null;
        }

        public string? GetTertiaryMood()
        {
            var moods = GetMoods();
            return moods.Count > 2 ? moods[2] : null;
        }

        // Helper methods for working with tags (stored as JSON array in Tags field)
        public List<string> GetTags()
        {
            if (string.IsNullOrEmpty(Tags))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(Tags) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public void SetTags(List<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                Tags = "";
                return;
            }

            Tags = JsonSerializer.Serialize(tags);
        }
    }
}
