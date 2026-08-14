using FluentValidation;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed class CriarEquipeCommandValidator : AbstractValidator<CriarEquipeCommand>
{
    public CriarEquipeCommandValidator()
    {
        RuleFor(x => x.NomeEquipe).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public sealed class AtualizarEquipeCommandValidator : AbstractValidator<AtualizarEquipeCommand>
{
    public AtualizarEquipeCommandValidator()
    {
        RuleFor(x => x.NomeEquipe).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public sealed class AdicionarMembroCommandValidator : AbstractValidator<AdicionarMembroCommand>
{
    public AdicionarMembroCommandValidator()
    {
        RuleFor(x => x.EquipeId).NotEmpty();
        RuleFor(x => x.UsuarioId).NotEmpty();
        RuleFor(x => x.Papel).IsInEnum()
            .NotEqual(Perfil.Admin).WithMessage("O papel do membro na equipe deve ser Gestor ou Analista.");
    }
}
