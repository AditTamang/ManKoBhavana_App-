using System.Text.Json.Serialization;

namespace ManKoBhavna.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public required string Email { get; set; }

        public required string Username { get; set; }

        public required string PasswordHash { get; set; }

        [JsonIgnore]
        public ICollection<JournalEntry> JournalEntries { get; set; } = new List<JournalEntry>();
    }
}
