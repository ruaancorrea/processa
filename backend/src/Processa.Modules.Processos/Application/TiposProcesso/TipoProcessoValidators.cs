using FluentValidation;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed class CriarTipoProcessoCommandValidator : AbstractValidator<CriarTipoProcessoCommand>
{
    public CriarTipoProcessoCommandValidator()
    {
        RuleFor(x => x.EquipeId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.ModoAtribuicao).IsInEnum();
        RuleFor(x => x.ResponsavelFixoId).NotEmpty()
            .WithMessage("Modo de atribuição fixo exige um responsável fixo.")
            .When(x => x.ModoAtribuicao == Domain.ModoAtribuicao.Fixo);
    }
}

public sealed class AtualizarTipoProcessoCommandValidator : AbstractValidator<AtualizarTipoProcessoCommand>
{
    public AtualizarTipoProcessoCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.ModoAtribuicao).IsInEnum();
        RuleFor(x => x.ResponsavelFixoId).NotEmpty()
            .WithMessage("Modo de atribuição fixo exige um responsável fixo.")
            .When(x => x.ModoAtribuicao == Domain.ModoAtribuicao.Fixo);
    }
}

public sealed class DefinirPermissoesInicioCommandValidator : AbstractValidator<DefinirPermissoesInicioCommand>
{
    public DefinirPermissoesInicioCommandValidator()
    {
        RuleFor(x => x.TipoProcessoId).NotEmpty();
        RuleForEach(x => x.Perfis).IsInEnum();
    }
}
