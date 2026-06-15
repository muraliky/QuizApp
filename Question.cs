namespace QuizApp
{
    public class Question
    {
        public string QuestionText { get; set; }
        public string[] Options { get; set; }
        public int CorrectAnswerIndex { get; set; }
    }
}
