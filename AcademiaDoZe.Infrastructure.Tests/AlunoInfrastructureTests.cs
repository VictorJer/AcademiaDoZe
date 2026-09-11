using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AlunoInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly AlunoRepository _alunoRepo;

    public AlunoInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _alunoRepo = new AlunoRepository(ConnectionString, DatabaseType);
    }

    internal static async Task<Aluno> CriarEInserirAlunoAsync(AlunoRepository alunoRepo, LogradouroRepository logradouroRepo)
    {
        var logradouro = await LogradouroInfrastructureTests.CriarEInserirLogradouroAsync(logradouroRepo);
        var foto = Arquivo.Criar(new byte[] { 1, 2, 3, 4 }).Value!;
        var alunoResult = Aluno.Criar(
            id: 0,
            nome: "Aluno Teste " + Guid.NewGuid().ToString("N")[..5],
            cpf: GerarCpf(),
            dataNascimento: new DateOnly(2001, 6, 20),
            telefone: GerarTelefone(),
            email: GerarEmail(),
            endereco: logradouro,
            numero: "123",
            complemento: "Casa",
            senha: "SenhaValida123",
            foto: foto
        );

        if (alunoResult.IsFailure)
        {
            throw new Exception($"Falha ao criar Aluno: {string.Join(", ", alunoResult.Notifications.Select(n => n.Mensagem))}");
        }

        return await alunoRepo.Adicionar(alunoResult.Value!);
    }

    [Fact]
    public async Task Aluno_Adicionar_E_ObterPorId_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        Assert.NotNull(aluno);
        Assert.True(aluno.Id > 0);

        var obtido = await _alunoRepo.ObterPorId(aluno.Id);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido.Id);
        Assert.Equal(aluno.Cpf.Valor, obtido.Cpf.Valor);
        Assert.Equal(aluno.Nome, obtido.Nome);
        Assert.Equal(aluno.Email.Valor, obtido.Email.Valor);
        Assert.Equal(aluno.Endereco.LogradouroId, obtido.Endereco.LogradouroId);
    }

    [Fact]
    public async Task Aluno_ObterPorId_RetornaNuloQuandoInexistente()
    {
        var obtido = await _alunoRepo.ObterPorId(999999);
        Assert.Null(obtido);
    }

    [Fact]
    public async Task Aluno_ObterTodos_Sucesso()
    {
        await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var todos = await _alunoRepo.ObterTodos();
        Assert.NotNull(todos);
        Assert.NotEmpty(todos);
    }

    [Fact]
    public async Task Aluno_Atualizar_Sucesso()
    {
        var logradouro = await LogradouroInfrastructureTests.CriarEInserirLogradouroAsync(_logradouroRepo);
        var foto = Arquivo.Criar(new byte[] { 9, 8, 7, 6 }).Value!;
        var aluno = await _alunoRepo.Adicionar(Aluno.Criar(
            0,
            "Aluno Original",
            GerarCpf(),
            new DateOnly(2000, 5, 15),
            GerarTelefone(),
            GerarEmail(),
            logradouro,
            "100",
            "Casa",
            "Senha123",
            foto
        ).Value!);

        var novoNome = "Aluno Editado " + Guid.NewGuid().ToString("N")[..5];
        var alunoAtualizado = Aluno.Criar(
            id: aluno.Id,
            nome: novoNome,
            cpf: aluno.Cpf.Valor,
            dataNascimento: aluno.DataNascimento,
            telefone: aluno.Telefone.Valor,
            email: aluno.Email.Valor,
            endereco: logradouro,
            numero: "200",
            complemento: "Apartamento",
            senha: aluno.Senha.Valor,
            foto: aluno.Foto
        ).Value!;

        var resultado = await _alunoRepo.Atualizar(alunoAtualizado);
        Assert.NotNull(resultado);
        Assert.Equal(novoNome, resultado.Nome);

        var noBanco = await _alunoRepo.ObterPorId(aluno.Id);
        Assert.NotNull(noBanco);
        Assert.Equal(novoNome, noBanco.Nome);
    }

    [Fact]
    public async Task Aluno_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var logradouro = await LogradouroInfrastructureTests.CriarEInserirLogradouroAsync(_logradouroRepo);
        var foto = Arquivo.Criar(new byte[] { 1, 2 }).Value!;
        var alunoInexistente = Aluno.Criar(
            id: 999999,
            nome: "Inexistente",
            cpf: GerarCpf(),
            dataNascimento: new DateOnly(1990, 1, 1),
            telefone: GerarTelefone(),
            email: GerarEmail(),
            endereco: logradouro,
            numero: "1",
            complemento: "",
            senha: "SenhaValida123",
            foto: foto
        ).Value!;

        var ex = await Assert.ThrowsAsync<InfrastructureException>(() => _alunoRepo.Atualizar(alunoInexistente));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", ex.ErrorCode);
    }

    [Fact]
    public async Task Aluno_Remover_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var removido = await _alunoRepo.Remover(aluno.Id);
        Assert.True(removido);

        var noBanco = await _alunoRepo.ObterPorId(aluno.Id);
        Assert.Null(noBanco);
    }

    [Fact]
    public async Task Aluno_Remover_RetornaFalseQuandoInexistente()
    {
        var removido = await _alunoRepo.Remover(999999);
        Assert.False(removido);
    }

    [Fact]
    public async Task Aluno_ObterPorCpf_SucessoENulo()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var obtido = await _alunoRepo.ObterPorCpf(aluno.Cpf);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido.Id);

        var cpfInexistente = Cpf.Criar(GerarCpf()).Value!;
        var naoObtido = await _alunoRepo.ObterPorCpf(cpfInexistente);
        Assert.Null(naoObtido);
    }

    [Fact]
    public async Task Aluno_ObterPorEmail_SucessoENulo()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var obtido = await _alunoRepo.ObterPorEmail(aluno.Email);
        Assert.NotNull(obtido);
        Assert.Equal(aluno.Id, obtido.Id);

        var emailInexistente = Email.Criar(GerarEmail()).Value!;
        var naoObtido = await _alunoRepo.ObterPorEmail(emailInexistente);
        Assert.Null(naoObtido);
    }

    [Fact]
    public async Task Aluno_CpfJaExiste_ValidacaoCorreta()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);

        var existe = await _alunoRepo.CpfJaExiste(aluno.Cpf);
        Assert.True(existe);

        var existeIgnorandoId = await _alunoRepo.CpfJaExiste(aluno.Cpf, aluno.Id);
        Assert.False(existeIgnorandoId);

        var cpfInedito = Cpf.Criar(GerarCpf()).Value!;
        var existeInedito = await _alunoRepo.CpfJaExiste(cpfInedito);
        Assert.False(existeInedito);
    }

    [Fact]
    public async Task Aluno_EmailJaExiste_ValidacaoCorreta()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);

        var existe = await _alunoRepo.EmailJaExiste(aluno.Email);
        Assert.True(existe);

        var existeIgnorandoId = await _alunoRepo.EmailJaExiste(aluno.Email, aluno.Id);
        Assert.False(existeIgnorandoId);

        var emailInedito = Email.Criar(GerarEmail()).Value!;
        var existeInedito = await _alunoRepo.EmailJaExiste(emailInedito);
        Assert.False(existeInedito);
    }

    [Fact]
    public async Task Aluno_ObterPorNome_FiltragemCorreta()
    {
        var nome = "Aluno Nome " + Guid.NewGuid().ToString("N")[..5];
        var logradouro = await LogradouroInfrastructureTests.CriarEInserirLogradouroAsync(_logradouroRepo);
        var foto = Arquivo.Criar(new byte[] { 3, 4, 5 }).Value!;
        var aluno = Aluno.Criar(0, nome, GerarCpf(), new DateOnly(2002, 3, 17), GerarTelefone(), GerarEmail(), logradouro, "456", "Bloco A", "SenhaValida123", foto).Value!;
        await _alunoRepo.Adicionar(aluno);

        var resultados = await _alunoRepo.ObterPorNome(nome);
        Assert.NotNull(resultados);
        Assert.Contains(resultados, a => a.Id == aluno.Id);

        var resultadosVazio = await _alunoRepo.ObterPorNome("NomeQueNaoExiste_123");
        Assert.Empty(resultadosVazio);
    }

    [Fact]
    public async Task Aluno_TrocarSenha_SucessoEFalha()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var novaSenha = Senha.Criar("NovaSenhaAluno123").Value!;

        var alterou = await _alunoRepo.TrocarSenha(aluno.Id, novaSenha);
        Assert.True(alterou);

        var atualizado = await _alunoRepo.ObterPorId(aluno.Id);
        Assert.NotNull(atualizado);
        Assert.Equal("NovaSenhaAluno123", atualizado.Senha.Valor);

        var alterouInexistente = await _alunoRepo.TrocarSenha(999999, novaSenha);
        Assert.False(alterouInexistente);
    }
}
