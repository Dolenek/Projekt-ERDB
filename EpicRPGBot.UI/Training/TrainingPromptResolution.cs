namespace EpicRPGBot.UI.Training
{
    public sealed class TrainingPromptResolution
    {
        public TrainingPromptResolution(
            bool isTrainingPrompt,
            bool isResolved,
            TrainingPromptKind kind,
            string answerText,
            string preferredButtonLabel,
            string summary)
        {
            IsTrainingPrompt = isTrainingPrompt;
            IsResolved = isResolved;
            Kind = kind;
            AnswerText = answerText ?? string.Empty;
            PreferredButtonLabel = preferredButtonLabel ?? string.Empty;
            Summary = summary ?? string.Empty;
        }

        public bool IsTrainingPrompt { get; }
        public bool IsResolved { get; }
        public TrainingPromptKind Kind { get; }
        public string AnswerText { get; }
        public string PreferredButtonLabel { get; }
        public string Summary { get; }
    }
}
