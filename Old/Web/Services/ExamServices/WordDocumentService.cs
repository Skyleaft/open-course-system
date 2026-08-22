using System.Text.RegularExpressions;
using CoreLib.BankSoal;
using CoreLib.Common;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components;

namespace Web.Services.ExamServices;

public class WordDocumentService(NavigationManager navigationManager):IDocx
{
    public async Task<DocxResult> ProcessDocxAsync(Stream stream, string fileName)
    {
        var result = new DocxResult
        {
            FileName = fileName,
        };

        var soalList = new List<Soal>();
        Soal currentSoal = null;
    
        using var doc = WordprocessingDocument.Open(stream, false);
        
        var numberingPart = doc.MainDocumentPart.NumberingDefinitionsPart;
        var document = doc.MainDocumentPart.Document;
        
        if (document== null)
            return result;
        
        // Dictionary to track numbering sequences
        var numberingSequences = new Dictionary<string, int>();
        var numberIteration = 0;
    
        // Process paragraphs
        foreach (var paragraph in document.Descendants<Paragraph>())
        {
            var numProps = paragraph.ParagraphProperties?.NumberingProperties;
            if (numProps != null)
            {
                var numId = numProps.NumberingId?.Val ?? 0;
                var level = numProps.NumberingLevelReference?.Val ?? 0; // Now properly gets the level
                
                // Get abstract numbering ID
                var abstractNumId = GetAbstractNumberingId(numberingPart, numId);
                
                // Get numbering format for this level
                //ar format = GetNumberingFormat(numberingPart, abstractNumId, level);
                var format = GetNumberingFormat(numberingPart, numId, level);
                
                // Generate the correct number text
                var numberText = GenerateNumberText(numberingSequences, abstractNumId, level, format);
                
                if (format == "decimal")
                {
                    // This is a question (Soal) 
                    numberIteration += 1;
                    currentSoal = new Soal
                    {
                        Id = Guid.NewGuid(),
                        Nomor = numberIteration,
                        Pertanyaan = GetParagraphText(paragraph),
                        PilihanJawaban = new List<SoalJawaban>(),
                        isMultipleJawaban = false // Default value, can be changed later
                    };
                    soalList.Add(currentSoal);
                }
                else if (format == "upperLetter" && currentSoal != null)
                {
                    // This is an answer option (SoalJawaban)
                    var jawaban = new SoalJawaban
                    {
                        Id = Guid.NewGuid(),
                        SoalId = currentSoal.Id,
                        Jawaban = GetParagraphText(paragraph),
                        IsBenar = false, // Default value, can be set to true for correct answers
                        Point = 0 // Default value, can be set appropriately
                    };
                    currentSoal.PilihanJawaban.Add(jawaban);
                }
            }
            else
            {
                var text = GetParagraphText(paragraph);
                string pattern = @"([A-Z])\.\s*(\d+)|([A-Z])\s*\(\s*(\d+)\s*\)";
                if (Regex.IsMatch(text, pattern))
                {
                    result.KunJaw.Add(new KunciJawaban()
                    {
                        Nomor = numberIteration,
                        Jawaban = text
                    });
                }
            }
            
        }

        result.Soals = soalList;
        
        
        //proses point
        try
        {
            foreach (var item in result.KunJaw)
        {
            var soal = result.Soals.FirstOrDefault(x => x.Nomor == item.Nomor);
            if (soal != null)
            {
                if (item.Jawaban.Contains(";"))
                {
                    var pointstr =item.Jawaban.Split(";");
                    List<int> pointData=new();
                    foreach (var point in pointstr)
                    {
                        var pointint = ExtractNumber(point);
                        pointData.Add(pointint);
                    }

                    if (soal.PilihanJawaban.Count > 0)
                    {
                        for (int i = 0; i < soal.PilihanJawaban.Count; i++)
                        {
                            soal.PilihanJawaban[i].Point = pointData[i];
                            if (pointData[i] > 0)
                            {
                                soal.isMultipleJawaban = true;
                            }
                            if (i == pointData.Count - 1)
                            {
                                soal.PilihanJawaban[i].IsBenar = true;
                            }
                        }
                    }
                }
                else if(item.Jawaban.Contains(","))
                {
                    var pointstr =item.Jawaban.Split(",");
                    List<int> pointData=new();
                    foreach (var point in pointstr)
                    {
                        var pointint = ExtractNumber(point);
                        pointData.Add(pointint);
                    }
                
                    if (soal.PilihanJawaban.Count > 0)
                    {
                        for (int i = 0; i < soal.PilihanJawaban.Count; i++)
                        {
                            soal.PilihanJawaban[i].Point = pointData[i];
                            if (pointData[i] > 0)
                            {
                                soal.isMultipleJawaban = true;
                            }
                            if (i == pointData.Count - 1)
                            {
                                soal.PilihanJawaban[i].IsBenar = true;
                            }
                        }
                    } 
                }
                result.Soals[result.Soals.FindIndex(x => x.Nomor == item.Nomor)] = soal;
            }
            
        }
        }
        catch (Exception ex)
        {
            throw new Exception("err " + ex);
        }
        

        var unprocSoal = result.Soals.Select(x => x.Nomor)
            .Where(x => result.KunJaw.Select(x => x.Nomor).Contains(x)==false).ToList();
        foreach (var item in unprocSoal)
        {
            result.UnprocesKunci.Add($"No. {item}");
        }

        result.TotalSoal = numberIteration;
    
        return result;
    }

    public async Task<string> GetDownloadUrl(string fileName)
    {
        return navigationManager.BaseUri + $"files/{fileName}";
    }

    private int ExtractNumber(string numberStr)
    {
        // Remove non-numeric characters from "1." or similar
        var numericPart = new string(numberStr.Where(char.IsDigit).ToArray());
        if (int.TryParse(numericPart, out int number))
        {
            return number;
        }
        return 0; // Default if parsing fails
    }
    
    private int GetAbstractNumberingId(NumberingDefinitionsPart numberingPart, int numId)
    {
        if (numberingPart == null) return -1;

        var numberingInstance = numberingPart.Numbering
            .Elements<NumberingInstance>()
            .FirstOrDefault(n => n.NumberID?.Value == numId);

        return numberingInstance?.AbstractNumId?.Val?.Value ?? -1;
    }
    
    static string GetParagraphText(Paragraph paragraph)
    {
        string text = "";
        
        foreach (Run run in paragraph.Elements<Run>())
        {
            string runText = string.Concat(run.Elements<Text>().Select(t => t.Text));
            if (string.IsNullOrEmpty(runText)) continue;

            var runProps = run.RunProperties;
            bool isBold = runProps?.Bold != null;
            bool isItalic = runProps?.Italic != null;
            bool isUnderline = runProps?.Underline != null && runProps.Underline.Val != UnderlineValues.None;

            // Wrap text with appropriate tags
            if (isBold) runText = $"<b>{runText}</b>";
            if (isItalic) runText = $"<i>{runText}</i>";
            if (isUnderline) runText = $"<u>{runText}</u>";

            text += runText;
        }
        
        return text;
    }
    
    private string GetNumberingFormat(NumberingDefinitionsPart numberingPart, int numId, int level)
    {
        if (numberingPart == null) return "decimal"; // default

        // Find the numbering instance
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

                return levelData?.NumberingFormat?.Val ?? "decimal";
            }
        }

        return "decimal"; // default
    }
    
    private string GenerateNumberText(Dictionary<string, int> sequences, int abstractNumId, int level, string format)
    {
        // Create a unique key for this numbering sequence
        string sequenceKey = $"{abstractNumId}_{level}";
    
        // Get or create sequence counter
        if (!sequences.ContainsKey(sequenceKey))
        {
            sequences[sequenceKey] = 0;
        }
        sequences[sequenceKey]++;

        // Format based on the numbering type
        switch (format)
        {
            case "decimal":
                return sequences[sequenceKey].ToString() + ".";
            case "upperLetter":
                return ((char)('A' + sequences[sequenceKey] - 1)).ToString() + ".";
            case "lowerLetter":
                return ((char)('a' + sequences[sequenceKey] - 1)).ToString() + ".";
            case "upperRoman":
                return ToRomanNumerals(sequences[sequenceKey]) + ".";
            case "lowerRoman":
                return ToRomanNumerals(sequences[sequenceKey]).ToLower() + ".";
            default:
                return sequences[sequenceKey].ToString() + ".";
        }
    }
    
    private static string ToRomanNumerals(int number)
    {
        if (number < 1 || number > 3999)
            return number.ToString();
            
        var romanNumerals = new Dictionary<int, string>
        {
            {1000, "M"}, {900, "CM"}, {500, "D"}, {400, "CD"},
            {100, "C"}, {90, "XC"}, {50, "L"}, {40, "XL"},
            {10, "X"}, {9, "IX"}, {5, "V"}, {4, "IV"}, {1, "I"}
        };
        
        var result = new System.Text.StringBuilder();
        
        foreach (var pair in romanNumerals)
        {
            while (number >= pair.Key)
            {
                result.Append(pair.Value);
                number -= pair.Key;
            }
        }
        
        return result.ToString();
    }
}