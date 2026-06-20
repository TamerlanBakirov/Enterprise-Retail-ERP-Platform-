using FluentValidation;
using GeorgiaERP.Application.Finance.Commands;

namespace GeorgiaERP.Application.Finance.Validators;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private static readonly string[] ValidTypes = ["Asset", "Liability", "Equity", "Revenue", "Expense"];
    private static readonly string[] ValidBalance = ["Debit", "Credit"];

    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.AccountCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameKa).MaximumLength(200);
        RuleFor(x => x.AccountType).NotEmpty().Must(t => ValidTypes.Contains(t))
            .WithMessage("AccountType must be one of: " + string.Join(", ", ValidTypes));
        RuleFor(x => x.BalanceType).NotEmpty().Must(t => ValidBalance.Contains(t))
            .WithMessage("BalanceType must be Debit or Credit.");
    }
}

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Iban).MaximumLength(34);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
    }
}

public class CreateJournalEntryCommandValidator : AbstractValidator<CreateJournalEntryCommand>
{
    public CreateJournalEntryCommandValidator()
    {
        RuleFor(x => x.EntryDate).NotEmpty();
        RuleFor(x => x.CreatedBy).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("At least one journal line is required.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.DebitAmount).GreaterThanOrEqualTo(0);
            line.RuleFor(l => l.CreditAmount).GreaterThanOrEqualTo(0);
        });
    }
}

public class PostJournalEntryCommandValidator : AbstractValidator<PostJournalEntryCommand>
{
    public PostJournalEntryCommandValidator()
    {
        RuleFor(x => x.JournalEntryId).NotEmpty();
        RuleFor(x => x.PostedBy).NotEmpty();
    }
}
