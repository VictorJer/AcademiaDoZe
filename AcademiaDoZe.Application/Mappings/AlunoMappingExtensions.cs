// Victor Jeremiasiago Kovalski
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;
namespace AcademiaDoZe.Application.Mappings;

public static class AlunoMappingExtensions
{
    public static AlunoDto ToDto(this Aluno aluno, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        return new AlunoDto
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Cpf = aluno.Cpf.Valor,
            DataNascimento = aluno.DataNascimento,
            Telefone = aluno.Telefone.Valor,
            Email = aluno.Email?.Valor,
            Endereco = logradouro?.ToDto(),
            Numero = aluno.Endereco?.Numero ?? string.Empty,
            Complemento = aluno.Endereco?.Complemento,
            Senha = null, // a senha não deve ser exposta no DTO
            Foto = aluno.Foto?.Conteudo != null ? new ArquivoDto { Conteudo = aluno.Foto.Conteudo } : null
        };
    }
    public static Aluno ToEntity(this AlunoDto alunoDto, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(alunoDto);
        var logradouroEntidade = logradouro ?? (alunoDto.Endereco != null ? alunoDto.Endereco.ToEntity() : null) ?? throw new InvalidOperationException("Logradouro/Endereço é obrigatório para converter o Aluno.");
        Arquivo? foto = null;
        if (alunoDto.Foto?.Conteudo != null)
        {
            var fotoResult = Arquivo.Criar(alunoDto.Foto.Conteudo);
            if (fotoResult.IsSuccess) foto = fotoResult.Value;
        }
        var result = Aluno.Criar(
        alunoDto.Id,
        alunoDto.Nome,
        alunoDto.Cpf,
        alunoDto.DataNascimento,
        alunoDto.Telefone,
        alunoDto.Email ?? string.Empty,
        logradouroEntidade,
        alunoDto.Numero,
        alunoDto.Complemento ?? string.Empty,
        alunoDto.Senha ?? string.Empty,
        foto!
        );
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Erro de validação ao converter Aluno: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }
    public static Aluno UpdateFromDto(this Aluno aluno, AlunoDto alunoDto, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        ArgumentNullException.ThrowIfNull(alunoDto);
        var logradouroEntidade = logradouro ?? (alunoDto.Endereco != null ? alunoDto.Endereco.ToEntity() : null) ?? throw new InvalidOperationException("Logradouro/Endereço é obrigatório para atualizar o Aluno.");
        Arquivo? foto = aluno.Foto;
        if (alunoDto.Foto?.Conteudo != null)
        {
            var fotoResult = Arquivo.Criar(alunoDto.Foto.Conteudo);
            if (fotoResult.IsSuccess) foto = fotoResult.Value;
        }
        string senha = !string.IsNullOrWhiteSpace(alunoDto.Senha) ? alunoDto.Senha : aluno.Senha.Valor;
        var result = Aluno.Criar(
        aluno.Id,
        alunoDto.Nome ?? aluno.Nome,
        aluno.Cpf.Valor,
        alunoDto.DataNascimento != default ? alunoDto.DataNascimento : aluno.DataNascimento,
        alunoDto.Telefone ?? aluno.Telefone.Valor,
        alunoDto.Email ?? aluno.Email.Valor,
        logradouroEntidade,
        alunoDto.Numero ?? aluno.Endereco.Numero,
        alunoDto.Complemento ?? aluno.Endereco.Complemento,
        senha,
        foto!
        );
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Erro de validação ao atualizar Aluno: {string.Join(", ", result.Notifications.Select(n => n.Mensagem))}");
        }
        return result.Value!;
    }
}