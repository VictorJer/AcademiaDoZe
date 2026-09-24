// Victor Jeremiasiago Kovalski
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Application.Mappings;

public static class AcessoMappingExtensions
{
    public static AcessoAlunoDto ToDto(this AcessoAluno acesso)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        return new AcessoAlunoDto
        {
            Id = acesso.Id,
            AlunoId = acesso.AlunoId,
            DataHora = acesso.DataHora
        };
    }

    public static AcessoColaboradorDto ToDto(this AcessoColaborador acesso)
    {
        ArgumentNullException.ThrowIfNull(acesso);
        return new AcessoColaboradorDto
        {
            Id = acesso.Id,
            ColaboradorId = acesso.ColaboradorId,
            DataHora = acesso.DataHora
        };
    }
}