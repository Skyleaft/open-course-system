using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MonoSlice.Modules.Exams.Domain;

namespace MonoSlice.Modules.Exams.Services;

public sealed partial class WordQuestionBankService : IWordQuestionBankService
{
    [GeneratedRegex(@"^(?:(?:Soal|Question)\s+)?(\d+)[\.\)]\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex QuestionNumberRegex();

    [GeneratedRegex(@"^(\*|\[x\]\s*)?([A-Ea-e])[\.\)]\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex OptionLetterRegex();

    [GeneratedRegex(@"^(?:Answer|Jawaban|Kunci|Key|Ans)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex AnswerKeyRegex();

    [GeneratedRegex(@"^(?:Points?|Point|Nilai|Skor|Score)\s*[:=]\s*(\d+(?:\.\d+)?)$", RegexOptions.IgnoreCase)]
    private static partial Regex PointsRegex();

    [GeneratedRegex(@"^(?:Grading(?:\s*Method)?|Metode(?:\s*Penilaian)?|Scoring)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex GradingMethodRegex();

    [GeneratedRegex(@"^(?:Explanation|Pembahasan|Penjelasan|Keterangan)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplanationRegex();

    [GeneratedRegex(@"^(?:Title|Judul|Bank\s*Title)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex TitleHeaderRegex();

    [GeneratedRegex(@"^(?:Category|Kategori)\s*[:=]\s*(.+)$", RegexOptions.IgnoreCase)]
    private static partial Regex CategoryHeaderRegex();

    [GeneratedRegex(@"^\[(?:Points?|Skor|Poin|Nilai)\s*[:=]\s*([+-]?\d+(?:\.\d+)?)\]\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex BracketOptionPointsRegex();

    [GeneratedRegex(@"^\(([+-]?\d+(?:\.\d+)?)\s*(?:pts|poin|points)?\)\s*(.*)$", RegexOptions.IgnoreCase)]
    private static partial Regex ParenOptionPointsRegex();

    public async Task<WordQuestionBankParseResult> ParseDocxAsync(Stream docxStream, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var warnings = new List<string>();
            var questions = new List<ParsedQuestionItem>();
            string? docTitle = null;
            string? docCategory = null;

            using var memoryStream = new MemoryStream();
            docxStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var doc = WordprocessingDocument.Open(memoryStream, false);
            var mainPart = doc.MainDocumentPart;
            if (mainPart?.Document?.Body is null)
            {
                warnings.Add("The Word document is empty or could not be read.");
                return new WordQuestionBankParseResult(docTitle, docCategory, questions, warnings);
            }

            var numberingPart = mainPart.NumberingDefinitionsPart;
            var paragraphs = mainPart.Document.Body.Descendants<Paragraph>().ToList();

            ParsedQuestionBuilder? currentBuilder = null;

            void FinalizeCurrentQuestion()
            {
                if (currentBuilder is not null && !string.IsNullOrWhiteSpace(currentBuilder.QuestionText))
                {
                    var item = currentBuilder.Build(questions.Count + 1);
                    questions.Add(item);
                }
                currentBuilder = null;
            }

            foreach (var paragraph in paragraphs)
            {
                var text = GetParagraphCleanText(paragraph).Trim();
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Check title/category headers in document metadata
                if (docTitle is null && TitleHeaderRegex().Match(text) is { Success: true } titleMatch)
                {
                    docTitle = titleMatch.Groups[1].Value.Trim();
                    continue;
                }
                if (docCategory is null && CategoryHeaderRegex().Match(text) is { Success: true } catMatch)
                {
                    docCategory = catMatch.Groups[1].Value.Trim();
                    continue;
                }

                // Check OpenXml Numbering properties
                var numProps = paragraph.ParagraphProperties?.NumberingProperties;
                string? numberingFormat = null;
                if (numProps != null && numberingPart != null)
                {
                    var numId = numProps.NumberingId?.Val?.Value ?? 0;
                    var level = numProps.NumberingLevelReference?.Val?.Value ?? 0;
                    numberingFormat = GetNumberingFormat(numberingPart, numId, level);
                }

                // If native numbering indicates decimal, or text matches question pattern:
                var qMatch = QuestionNumberRegex().Match(text);
                var isExplicitQuestion = qMatch.Success || numberingFormat == "decimal";

                // Check if line is an option
                var optMatch = OptionLetterRegex().Match(text);
                var isExplicitOption = (optMatch.Success || numberingFormat is "upperLetter" or "lowerLetter") && currentBuilder != null;

                // Check answer key line
                var ansMatch = AnswerKeyRegex().Match(text);
                // Check points line
                var ptsMatch = PointsRegex().Match(text);
                // Check grading method line
                var gradeMatch = GradingMethodRegex().Match(text);
                // Check explanation line
                var expMatch = ExplanationRegex().Match(text);

                if (ansMatch.Success && currentBuilder != null)
                {
                    currentBuilder.AnswerKeyRaw = ansMatch.Groups[1].Value.Trim();
                }
                else if (ptsMatch.Success && currentBuilder != null)
                {
                    if (decimal.TryParse(ptsMatch.Groups[1].Value, out var pts))
                    {
                        currentBuilder.Points = pts;
                    }
                }
                else if (gradeMatch.Success && currentBuilder != null)
                {
                    currentBuilder.GradingMethodRaw = gradeMatch.Groups[1].Value.Trim();
                }
                else if (expMatch.Success && currentBuilder != null)
                {
                    currentBuilder.Explanation = expMatch.Groups[1].Value.Trim();
                }
                else if (isExplicitOption)
                {
                    var isStarred = optMatch.Success && !string.IsNullOrEmpty(optMatch.Groups[1].Value);
                    var optionRawText = optMatch.Success ? optMatch.Groups[3].Value.Trim() : text;
                    var letter = optMatch.Success ? optMatch.Groups[2].Value.ToUpperInvariant() : GetOptionLetter(currentBuilder!.Options.Count);

                    decimal? explicitPoints = null;
                    var cleanText = optionRawText;

                    var bracketMatch = BracketOptionPointsRegex().Match(optionRawText);
                    if (bracketMatch.Success)
                    {
                        if (decimal.TryParse(bracketMatch.Groups[1].Value, out var p))
                        {
                            explicitPoints = p;
                        }
                        cleanText = bracketMatch.Groups[2].Value.Trim();
                    }
                    else
                    {
                        var parenMatch = ParenOptionPointsRegex().Match(optionRawText);
                        if (parenMatch.Success)
                        {
                            if (decimal.TryParse(parenMatch.Groups[1].Value, out var p))
                            {
                                explicitPoints = p;
                            }
                            cleanText = parenMatch.Groups[2].Value.Trim();
                        }
                    }

                    currentBuilder!.Options.Add(new OptionDraft
                    {
                        Letter = letter,
                        Text = string.IsNullOrWhiteSpace(cleanText) ? optionRawText : cleanText,
                        IsMarkedCorrect = isStarred,
                        ExplicitPoints = explicitPoints
                    });
                }
                else if (isExplicitQuestion)
                {
                    FinalizeCurrentQuestion();
                    var qText = qMatch.Success ? qMatch.Groups[2].Value.Trim() : text;
                    currentBuilder = new ParsedQuestionBuilder
                    {
                        QuestionText = string.IsNullOrWhiteSpace(qText) ? text : qText
                    };
                }
                else if (currentBuilder != null)
                {
                    // Continuation text for question prompt or option
                    if (currentBuilder.Options.Count == 0)
                    {
                        currentBuilder.QuestionText += "\n" + text;
                    }
                    else
                    {
                        var lastOption = currentBuilder.Options.Last();
                        lastOption.Text += " " + text;
                    }
                }
            }

            FinalizeCurrentQuestion();

            if (questions.Count == 0)
            {
                warnings.Add("No valid questions could be detected. Please ensure questions are numbered (e.g. 1. Question) with choices (A. Option, B. Option).");
            }

            return new WordQuestionBankParseResult(docTitle, docCategory, questions, warnings);
        }, ct);
    }

    public byte[] GenerateTemplateDocx()
    {
        using var memoryStream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = new Body();

            // Document Header / Title
            body.Append(CreateHeadingParagraph("Question Bank Import Template", 32, "1E3A8A", true));
            body.Append(CreateParagraph("Title: Sample Certification Question Bank", true, "334155"));
            body.Append(CreateParagraph("Category: General Computer Science", true, "334155"));
            body.Append(CreateParagraph("Guidelines: Use standard numbering for questions (1., 2.) and choices (A., B., C., D.). Specify correct answers using 'Answer: A' or by putting an asterisk '*' before the choice like '*A.'. For Multiple Choice Multiple Answer, use 'Answer: A, C' or mark multiple choices with '*'. Optional per-question grading method: 'Grading: PartialWithPenalty' or 'Grading: AllOrNothing'. Optional per-option points: 'A. [Points: 5] Text'.", false, "64748B"));
            body.Append(CreateHorizontalRule());

            // Question 1: Single Choice with Answer: Key
            body.Append(CreateHeadingParagraph("1. Which of the following data structures operates on a First-In-First-Out (FIFO) principle?", 24, "0F172A", true));
            body.Append(CreateParagraph("A. Stack", false));
            body.Append(CreateParagraph("B. Queue", false));
            body.Append(CreateParagraph("C. Binary Search Tree", false));
            body.Append(CreateParagraph("D. Priority Queue", false));
            body.Append(CreateParagraph("Answer: B", true, "16A34A"));
            body.Append(CreateParagraph("Points: 2", false, "475569"));
            body.Append(CreateParagraph("Explanation: A Queue follows the First-In-First-Out (FIFO) principle where elements are inserted at the back and removed from the front.", false, "475569"));
            body.Append(CreateSpacer());

            // Question 2: Single Choice with Asterisk * Choice
            body.Append(CreateHeadingParagraph("2. What is the average time complexity of searching in a balanced Binary Search Tree (AVL Tree)?", 24, "0F172A", true));
            body.Append(CreateParagraph("*A. O(log n)", false));
            body.Append(CreateParagraph("B. O(n)", false));
            body.Append(CreateParagraph("C. O(1)", false));
            body.Append(CreateParagraph("D. O(n log n)", false));
            body.Append(CreateParagraph("Points: 2", false, "475569"));
            body.Append(CreateParagraph("Explanation: Balanced BSTs guarantee logarithmic depth, leading to O(log n) search time complexity.", false, "475569"));
            body.Append(CreateSpacer());

            // Question 3: Multiple Choice Multi-Answer with Proportional Grading
            body.Append(CreateHeadingParagraph("3. Which of the following HTTP status codes indicate a client-side error? (Select all that apply)", 24, "0F172A", true));
            body.Append(CreateParagraph("A. 400 Bad Request", false));
            body.Append(CreateParagraph("B. 200 OK", false));
            body.Append(CreateParagraph("C. 404 Not Found", false));
            body.Append(CreateParagraph("D. 500 Internal Server Error", false));
            body.Append(CreateParagraph("Answer: A, C", true, "16A34A"));
            body.Append(CreateParagraph("Grading: PartialWithPenalty", false, "2563EB"));
            body.Append(CreateParagraph("Points: 4", false, "475569"));
            body.Append(CreateParagraph("Explanation: 4xx series status codes indicate client-side errors, whereas 5xx are server errors and 2xx are successful requests.", false, "475569"));
            body.Append(CreateSpacer());

            // Question 4: Option Weighted Question (Survey / Likert / Granular Score)
            body.Append(CreateHeadingParagraph("4. How frequently does your engineering team perform automated regression tests?", 24, "0F172A", true));
            body.Append(CreateParagraph("A. [Points: 5] On every commit via CI/CD pipeline", false));
            body.Append(CreateParagraph("B. [Points: 3] Nightly scheduled regression suites", false));
            body.Append(CreateParagraph("C. [Points: 1] Manually before major production releases", false));
            body.Append(CreateParagraph("D. [Points: 0] We do not have automated regression tests", false));
            body.Append(CreateParagraph("Grading: OptionWeighted", false, "2563EB"));
            body.Append(CreateParagraph("Points: 5", false, "475569"));
            body.Append(CreateParagraph("Explanation: Continuous testing on every commit reflects the highest DevOps maturity level.", false, "475569"));
            body.Append(CreateSpacer());

            // Question 5: True / False Question
            body.Append(CreateHeadingParagraph("5. In relational databases, a foreign key can only reference the primary key of another table, never a candidate unique key.", 24, "0F172A", true));
            body.Append(CreateParagraph("A. True", false));
            body.Append(CreateParagraph("B. False", false));
            body.Append(CreateParagraph("Answer: False", true, "16A34A"));
            body.Append(CreateParagraph("Points: 1", false, "475569"));
            body.Append(CreateParagraph("Explanation: A foreign key can reference any column with a UNIQUE constraint, not just primary keys.", false, "475569"));
            body.Append(CreateSpacer());

            // Question 6: Essay Question
            body.Append(CreateHeadingParagraph("6. Explain the concept of ACID properties in Database Management Systems and briefly describe each property.", 24, "0F172A", true));
            body.Append(CreateParagraph("Points: 5", false, "475569"));
            body.Append(CreateParagraph("Explanation: ACID stands for Atomicity (all-or-nothing), Consistency (state validity), Isolation (concurrent execution independence), and Durability (permanent persistence after commit).", false, "475569"));

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return memoryStream.ToArray();
    }

    private static Paragraph CreateHeadingParagraph(string text, int fontSizeHalfPts, string hexColor, bool bold)
    {
        var runProps = new RunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = fontSizeHalfPts.ToString() });
        runProps.Append(new Color { Val = hexColor });
        runProps.Append(new RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI" });

        var run = new Run();
        run.Append(runProps);
        run.Append(new Text(text));

        var pProps = new ParagraphProperties(new SpacingBetweenLines { Before = "120", After = "80" });
        var p = new Paragraph(pProps);
        p.Append(run);
        return p;
    }

    private static Paragraph CreateParagraph(string text, bool bold = false, string hexColor = "1E293B")
    {
        var runProps = new RunProperties();
        if (bold) runProps.Append(new Bold());
        runProps.Append(new FontSize { Val = "21" }); // ~10.5pt
        runProps.Append(new Color { Val = hexColor });
        runProps.Append(new RunFonts { Ascii = "Segoe UI", HighAnsi = "Segoe UI" });

        var run = new Run();
        run.Append(runProps);
        run.Append(new Text(text));

        var pProps = new ParagraphProperties(new SpacingBetweenLines { After = "60" });
        var p = new Paragraph(pProps);
        p.Append(run);
        return p;
    }

    private static Paragraph CreateSpacer()
    {
        return new Paragraph(new ParagraphProperties(new SpacingBetweenLines { After = "120" }));
    }

    private static Paragraph CreateHorizontalRule()
    {
        var pProps = new ParagraphProperties(
            new ParagraphBorders(new BottomBorder { Val = BorderValues.Single, Size = 6, Space = 1, Color = "CBD5E1" }),
            new SpacingBetweenLines { After = "140" }
        );
        return new Paragraph(pProps);
    }

    private static string GetParagraphCleanText(Paragraph paragraph)
    {
        return string.Concat(paragraph.Elements<Run>().SelectMany(r => r.Elements<Text>().Select(t => t.Text)));
    }

    private static string GetOptionLetter(int index)
    {
        return ((char)('A' + Math.Min(25, index))).ToString();
    }

    private static string GetNumberingFormat(NumberingDefinitionsPart numberingPart, int numId, int level)
    {
        if (numberingPart == null) return "decimal";

        var numberingInstance = numberingPart.Numbering
            .Elements<NumberingInstance>()
            .FirstOrDefault(n => n.NumberID?.Value == numId);

        if (numberingInstance != null)
        {
            var abstractNumId = numberingInstance.AbstractNumId?.Val?.Value;
            var abstractNum = numberingPart.Numbering
                .Elements<AbstractNum>()
                .FirstOrDefault(a => a.AbstractNumberId?.Value == abstractNumId);

            if (abstractNum != null)
            {
                var levelOverride = abstractNum
                    .Elements<LevelOverride>()
                    .FirstOrDefault(o => o.LevelIndex?.Value == level);

                var levelData = levelOverride?.Level ?? abstractNum
                    .Elements<Level>()
                    .FirstOrDefault(l => l.LevelIndex?.Value == level);
                var formatVal = levelData?.NumberingFormat?.Val;
                return formatVal is not null ? formatVal.Value.ToString() : "decimal";
            }
        }

        return "decimal";
    }

    private sealed class OptionDraft
    {
        public string Letter { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsMarkedCorrect { get; set; }
        public decimal? ExplicitPoints { get; set; }
        public decimal? ExplicitPenalty { get; set; }
    }

    private sealed class ParsedQuestionBuilder
    {
        public string QuestionText { get; set; } = string.Empty;
        public decimal Points { get; set; } = 1m;
        public string? Explanation { get; set; }
        public string? AnswerKeyRaw { get; set; }
        public string? GradingMethodRaw { get; set; }
        public List<OptionDraft> Options { get; set; } = [];

        public ParsedQuestionItem Build(int questionIndex)
        {
            var parsedOptions = new List<ParsedOptionItem>();

            // Parse answer key letters (e.g., "A", "B, C", "False", "True")
            var correctLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool isTrueFalse = false;
            bool? trueFalseAnswer = null;

            if (!string.IsNullOrWhiteSpace(AnswerKeyRaw))
            {
                var raw = AnswerKeyRaw.Trim();
                if (raw.Equals("True", StringComparison.OrdinalIgnoreCase) || raw.Equals("Benar", StringComparison.OrdinalIgnoreCase))
                {
                    isTrueFalse = true;
                    trueFalseAnswer = true;
                }
                else if (raw.Equals("False", StringComparison.OrdinalIgnoreCase) || raw.Equals("Salah", StringComparison.OrdinalIgnoreCase))
                {
                    isTrueFalse = true;
                    trueFalseAnswer = false;
                }
                else
                {
                    var tokens = raw.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries);
                    foreach (var token in tokens)
                    {
                        var clean = token.Trim().TrimEnd('.', ')');
                        if (!string.IsNullOrEmpty(clean))
                        {
                            correctLetters.Add(clean);
                        }
                    }
                }
            }

            // Check if options are True/False
            if (Options.Count == 2 &&
                (Options.Any(o => o.Text.Equals("True", StringComparison.OrdinalIgnoreCase)) ||
                 Options.Any(o => o.Text.Equals("Benar", StringComparison.OrdinalIgnoreCase))))
            {
                isTrueFalse = true;
            }

            bool hasExplicitOptionPoints = Options.Any(o => o.ExplicitPoints.HasValue);

            foreach (var opt in Options)
            {
                var isCorrect = opt.IsMarkedCorrect || correctLetters.Contains(opt.Letter);
                if (isTrueFalse && trueFalseAnswer.HasValue)
                {
                    if (trueFalseAnswer.Value && (opt.Text.Equals("True", StringComparison.OrdinalIgnoreCase) || opt.Text.Equals("Benar", StringComparison.OrdinalIgnoreCase)))
                    {
                        isCorrect = true;
                    }
                    else if (!trueFalseAnswer.Value && (opt.Text.Equals("False", StringComparison.OrdinalIgnoreCase) || opt.Text.Equals("Salah", StringComparison.OrdinalIgnoreCase)))
                    {
                        isCorrect = true;
                    }
                }

                decimal optPoints = opt.ExplicitPoints ?? (isCorrect ? Points : 0m);
                decimal optPenalty = opt.ExplicitPenalty ?? 0m;

                parsedOptions.Add(new ParsedOptionItem(
                    opt.Text.Trim(),
                    isCorrect,
                    optPoints,
                    optPenalty
                ));
            }

            // Determine question type
            QuestionType type;
            if (parsedOptions.Count == 0)
            {
                type = QuestionType.Essay;
            }
            else if (isTrueFalse)
            {
                type = QuestionType.TrueFalse;
            }
            else
            {
                var correctCount = parsedOptions.Count(o => o.IsCorrect);
                type = correctCount > 1 ? QuestionType.MultipleChoice : QuestionType.SingleChoice;

                // Fallback: If no option was marked correct and no explicit points, default first option as correct
                if (correctCount == 0 && !hasExplicitOptionPoints && parsedOptions.Count > 0)
                {
                    parsedOptions[0] = parsedOptions[0] with { IsCorrect = true, Points = Points };
                }
            }

            // Determine Grading Method
            GradingMethod gradingMethod;
            if (!string.IsNullOrWhiteSpace(GradingMethodRaw))
            {
                var rawGrade = GradingMethodRaw.Trim().ToLowerInvariant();
                if (rawGrade.Contains("allornothing") || rawGrade.Contains("all_or_nothing") || rawGrade == "all")
                {
                    gradingMethod = GradingMethod.AllOrNothing;
                }
                else if (rawGrade.Contains("partialwithoutpenalty") || rawGrade.Contains("withoutpenalty") || rawGrade.Contains("tanpapenalti"))
                {
                    gradingMethod = GradingMethod.PartialWithoutPenalty;
                }
                else if (rawGrade.Contains("optionweighted") || rawGrade.Contains("weighted") || rawGrade.Contains("bobot"))
                {
                    gradingMethod = GradingMethod.OptionWeighted;
                }
                else
                {
                    gradingMethod = GradingMethod.PartialWithPenalty;
                }
            }
            else if (hasExplicitOptionPoints)
            {
                gradingMethod = GradingMethod.OptionWeighted;
            }
            else if (type == QuestionType.MultipleChoice)
            {
                gradingMethod = GradingMethod.PartialWithPenalty;
            }
            else
            {
                gradingMethod = GradingMethod.AllOrNothing;
            }

            return new ParsedQuestionItem(
                questionIndex,
                QuestionText.Trim(),
                type,
                Points,
                Explanation,
                parsedOptions,
                gradingMethod
            );
        }
    }
}
