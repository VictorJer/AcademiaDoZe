// Victor Jeremiasiago Kovalski
using AcademiaDoZe.Application.Enums;

namespace AcademiaDoZe.Application.DTOs;

public class ColaboradorDto : PessoaDto
{
    public required DateOnly DataAdmissao { get; set; }
    public required AppColaboradorTipo Tipo { get; set; }
    public required AppColaboradorVinculo Vinculo { get; set; }
    public string TipoDescricao => Tipo.GetDisplayName();
    public string VinculoDescricao => Vinculo.GetDisplayName();
}