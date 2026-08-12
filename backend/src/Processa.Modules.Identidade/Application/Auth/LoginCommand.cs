using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Auth;

public sealed record LoginCommand(string Email, string Senha) : IRequest<Result<LoginResultado>>;

public sealed record LoginResultado(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    string RefreshTokenOpaco,
    Guid UsuarioId,
    Guid TenantId,
    Perfil Perfil);

public sealed class LoginCommandHandler(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork) : IRequestHandler<LoginCommand, Result<LoginResultado>>
{
    // Mensagem de erro é sempre a mesma para "não existe" e "senha errada" —
    // não revelar qual das duas para não ajudar enumeração de e-mails cadastrados.
    private const string ErroCredenciaisInvalidas = "E-mail ou senha inválidos.";

    // Hash bcrypt fixo (sem senha real correspondente), usado só para consumir tempo de
    // CPU equivalente ao caminho "usuário existe" quando o usuário não é encontrado —
    // sem isso, a resposta para e-mail inexistente volta muito mais rápido que para senha
    // errada (que roda bcrypt de verdade), um timing side-channel que permite enumerar
    // e-mails cadastrados medindo a latência da resposta.
    private const string HashFicticioParaMitigarTimingAttack =
        "$2a$12$nCQ4RBU0A8pz9WwaRiZ7b.JyLMdDCNvXz5aUMCAHzv24fET2BsF2a";

    public async Task<Result<LoginResultado>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Criar(request.Email);
        if (emailResult.IsFailure)
            return Result.Failure<LoginResultado>(ErroCredenciaisInvalidas);

        var usuario = await usuarioRepository.ObterPorEmailAsync(emailResult.Value, cancellationToken);
        if (usuario is null || !usuario.Ativo)
        {
            passwordHasher.Verificar(request.Senha, HashFicticioParaMitigarTimingAttack);
            return Result.Failure<LoginResultado>(ErroCredenciaisInvalidas);
        }

        if (usuario.EstaBloqueado())
            return Result.Failure<LoginResultado>(
                $"Conta bloqueada por tentativas de login inválidas. Tente novamente após {usuario.BloqueadoAte:HH:mm}.");

        if (!passwordHasher.Verificar(request.Senha, usuario.SenhaHash))
        {
            usuario.RegistrarTentativaFalha();
            await unitOfWork.SalvarAsync(cancellationToken);
            return Result.Failure<LoginResultado>(ErroCredenciaisInvalidas);
        }

        usuario.RegistrarLoginSucesso();

        var accessToken = tokenGenerator.GerarAccessToken(usuario);
        var refreshTokenOpaco = tokenGenerator.GerarRefreshTokenOpaco();
        var refreshTokenHash = tokenGenerator.HashDoRefreshToken(refreshTokenOpaco);

        await refreshTokenRepository.AddAsync(RefreshToken.Criar(usuario.Id, refreshTokenHash), cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(new LoginResultado(
            accessToken.Token,
            accessToken.ExpiraEm,
            refreshTokenOpaco,
            usuario.Id,
            usuario.TenantId,
            usuario.Perfil));
    }
}
