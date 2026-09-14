using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class MatriculaInfrastructureTests : TestBase
{
    private readonly LogradouroRepository _logradouroRepo;
    private readonly AlunoRepository _alunoRepo;
    private readonly MatriculaRepository _matriculaRepo;

    public MatriculaInfrastructureTests()
    {
        _logradouroRepo = new LogradouroRepository(ConnectionString, DatabaseType);
        _alunoRepo = new AlunoRepository(ConnectionString, DatabaseType);
        _matriculaRepo = new MatriculaRepository(ConnectionString, DatabaseType);
    }

    internal static async Task<Aluno> CriarEInserirAlunoAsync(AlunoRepository alunoRepo, LogradouroRepository logradouroRepo)
    {
        var logradouro = await LogradouroInfrastructureTests.CriarEInserirLogradouroAsync(logradouroRepo);
        var foto = Arquivo.Criar(new byte[] { 1, 2, 3, 4 }).Value!;
        var alunoResult = Aluno.Criar(
            id: 0,
            nome: "Aluno Matricula " + Guid.NewGuid().ToString("N")[..5],
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

    internal static async Task<Matricula> CriarEInserirMatriculaAsync(MatriculaRepository matriculaRepo, AlunoRepository alunoRepo, LogradouroRepository logradouroRepo)
    {
        var aluno = await CriarEInserirAlunoAsync(alunoRepo, logradouroRepo);
        var laudo = Arquivo.Criar(new byte[] { 5, 6, 7 }).Value!;
        var matriculaResult = Matricula.Criar(
            id: 0,
            aluno: aluno,
            plano: MatriculaPlano.Mensal,
            dataInicio: DateOnly.FromDateTime(DateTime.Today),
            objetivo: "Melhorar condicionamento",
            restricoesMedicas: MatriculaRestricoes.Diabetes,
            laudoMedico: laudo,
            observacoesRestricoes: "Observações de teste"
        );

        if (matriculaResult.IsFailure)
        {
            throw new Exception($"Falha ao criar Matrícula: {string.Join(", ", matriculaResult.Notifications.Select(n => n.Mensagem))}");
        }

        return await matriculaRepo.Adicionar(matriculaResult.Value!);
    }

    [Fact]
    public async Task Matricula_Adicionar_E_ObterPorId_Sucesso()
    {
        var matricula = await CriarEInserirMatriculaAsync(_matriculaRepo, _alunoRepo, _logradouroRepo);

        Assert.NotNull(matricula);
        Assert.True(matricula.Id > 0);
        Assert.Equal(matricula.AlunoId, matricula.AlunoId);

        var obtida = await _matriculaRepo.ObterPorId(matricula.Id);
        Assert.NotNull(obtida);
        Assert.Equal(matricula.Id, obtida.Id);
        Assert.Equal(matricula.Plano, obtida.Plano);
        Assert.Equal(matricula.Objetivo, obtida.Objetivo);
    }

    [Fact]
    public async Task Matricula_ObterPorId_RetornaNuloQuandoInexistente()
    {
        var obtida = await _matriculaRepo.ObterPorId(999999);
        Assert.Null(obtida);
    }

    [Fact]
    public async Task Matricula_ObterTodos_Sucesso()
    {
        await CriarEInserirMatriculaAsync(_matriculaRepo, _alunoRepo, _logradouroRepo);
        var todas = await _matriculaRepo.ObterTodos();
        Assert.NotNull(todas);
        Assert.NotEmpty(todas);
    }

    [Fact]
    public async Task Matricula_Atualizar_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var laudo = Arquivo.Criar(new byte[] { 9, 8, 7 }).Value!;

        var matricula = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today),
            "Objetivo original",
            MatriculaRestricoes.None,
            null,
            string.Empty
        ).Value!);

        var atualizada = Matricula.Criar(
            matricula.Id,
            aluno,
            MatriculaPlano.Trimestral,
            matricula.DataInicio,
            "Objetivo atualizado",
            MatriculaRestricoes.Diabetes,
            laudo,
            "Observações atualizadas"
        ).Value!;

        var resultado = await _matriculaRepo.Atualizar(atualizada);
        Assert.NotNull(resultado);
        Assert.Equal(MatriculaPlano.Trimestral, resultado.Plano);
        Assert.Equal("Objetivo atualizado", resultado.Objetivo);

        var noBanco = await _matriculaRepo.ObterPorId(matricula.Id);
        Assert.NotNull(noBanco);
        Assert.Equal(MatriculaPlano.Trimestral, noBanco.Plano);
    }

    [Fact]
    public async Task Matricula_Atualizar_LancaExcecaoQuandoInexistente()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var matriculaInexistente = Matricula.Criar(
            999999,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today),
            "Objetivo inexistente",
            MatriculaRestricoes.None,
            null,
            string.Empty
        ).Value!;

        var ex = await Assert.ThrowsAsync<InfrastructureException>(() => _matriculaRepo.Atualizar(matriculaInexistente));
        Assert.Equal("REGISTRO_NAO_ENCONTRADO", ex.ErrorCode);
    }

    [Fact]
    public async Task Matricula_Remover_Sucesso()
    {
        var matricula = await CriarEInserirMatriculaAsync(_matriculaRepo, _alunoRepo, _logradouroRepo);
        var removida = await _matriculaRepo.Remover(matricula.Id);
        Assert.True(removida);

        var noBanco = await _matriculaRepo.ObterPorId(matricula.Id);
        Assert.Null(noBanco);
    }

    [Fact]
    public async Task Matricula_Remover_RetornaFalseQuandoInexistente()
    {
        var removida = await _matriculaRepo.Remover(999999);
        Assert.False(removida);
    }

    [Fact]
    public async Task Matricula_ObterPorAluno_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var primeira = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today),
            "Objetivo 1",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Trimestral,
            DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            "Objetivo 2",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var resultados = await _matriculaRepo.ObterPorAluno(aluno.Id);
        Assert.NotNull(resultados);
        Assert.Contains(resultados, m => m.Id == primeira.Id);
        Assert.Equal(2, resultados.Count());
    }

    [Fact]
    public async Task Matricula_ObterMatriculaAtivaPorAluno_SucessoENulo()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var matricula = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            "Objetivo ativo",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var ativa = await _matriculaRepo.ObterMatriculaAtivaPorAluno(aluno.Id);
        Assert.NotNull(ativa);
        Assert.Equal(matricula.Id, ativa.Id);

        var alunoSemMatricula = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var semAtiva = await _matriculaRepo.ObterMatriculaAtivaPorAluno(alunoSemMatricula.Id);
        Assert.Null(semAtiva);
    }

    [Fact]
    public async Task Matricula_PossuiMatriculaAtiva_ValidacaoCorreta()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var possui = await _matriculaRepo.PossuiMatriculaAtiva(aluno.Id);
        Assert.False(possui);

        await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            "Objetivo",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var possuiAgora = await _matriculaRepo.PossuiMatriculaAtiva(aluno.Id);
        Assert.True(possuiAgora);
    }

    [Fact]
    public async Task Matricula_ObterAtivas_ParametroAlunoOpcional()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var ativa = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            "Objetivo ativo",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var todasAtivas = await _matriculaRepo.ObterAtivas();
        Assert.NotNull(todasAtivas);
        Assert.Contains(todasAtivas, m => m.Id == ativa.Id);

        var porAluno = await _matriculaRepo.ObterAtivas(aluno.Id);
        Assert.NotNull(porAluno);
        Assert.Contains(porAluno, m => m.Id == ativa.Id);
    }

    [Fact]
    public async Task Matricula_ObterVencendoEmDias_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var matricula = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Mensal,
            DateOnly.FromDateTime(DateTime.Today.AddDays(-20)),
            "Vence em breve",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var proximas = await _matriculaRepo.ObterVencendoEmDias(15);
        Assert.NotNull(proximas);
        Assert.Contains(proximas, m => m.Id == matricula.Id);
    }

    [Fact]
    public async Task Matricula_ObterPorPlano_Sucesso()
    {
        var aluno = await CriarEInserirAlunoAsync(_alunoRepo, _logradouroRepo);
        var matricula = await _matriculaRepo.Adicionar(Matricula.Criar(
            0,
            aluno,
            MatriculaPlano.Trimestral,
            DateOnly.FromDateTime(DateTime.Today),
            "Objetivo trimestral",
            MatriculaRestricoes.None,
            null,
            ""
        ).Value!);

        var resultados = await _matriculaRepo.ObterPorPlano(MatriculaPlano.Trimestral);
        Assert.NotNull(resultados);
        Assert.Contains(resultados, m => m.Id == matricula.Id);
    }
}
