using System.Collections.Generic;

namespace CyberAwarenessBot.Models
{
    public class QuizQuestion
    {
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();
        public int CorrectIndex { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public bool IsTrueFalse { get; set; }
    }
}
