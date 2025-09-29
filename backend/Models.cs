using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models
{
    public class Pokemon
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public Dictionary<string, double> Type { get; set; } = new();

        [JsonPropertyName("color")]
        public Dictionary<string, double> Color { get; set; } = new();

        [JsonPropertyName("other")]
        public Dictionary<string, double> Other { get; set; } = new();

        public override string ToString()
        {
            return $"Pokemon: {Name}";
        }
    }


    public class Question
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("targetAttribute")]
        public string TargetAttribute { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Question: {Text} (Category: {Category})";
        }
    }

    public enum QuestionCategory
    {
        Type,
        Color,
        Other
    }


    public class UserAnswer
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TargetAttribute { get; set; } = string.Empty;
        public UserResponse Response { get; set; }
        public double Certainty { get; set; } = 0.8; // Default moderate certainty
    }

    public enum UserResponse
    {
        Yes,
        Somewhat,
        NotReally,
        No,
        DontKnow
    }


}