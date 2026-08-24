using CoreLib.BankSoal;
using FluentValidation;

namespace Web.Services.ExamServices.Endpoints;

public class ExamValidator : Validator<Exam>
{
    public ExamValidator()
    {
        RuleForEach(x => x.Soals).SetValidator(new SoalValidator());
    }
}

public class SoalValidator : Validator<Soal>
{
    public SoalValidator()
    {
        RuleFor(x => x.Pertanyaan)
            .NotNull()
            .WithMessage("Pertanyaan tidak Boleh Kosong");
    }
}