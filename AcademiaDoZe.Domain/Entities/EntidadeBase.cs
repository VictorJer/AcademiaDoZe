using AcademiaDoZe.Domain.Exceptions;
namespace AcademiaDoZe.Domain.Entities;

// Classe base para todas as entidades, garantindo identidade única e validação de Id
public abstract class EntidadeBase
{
    public int Id { get; protected set; }
    protected EntidadeBase(int id = 0)
    {
        if (id < 0)
            throw new DomainException("ID_NEGATIVO");
        Id = id;
    }
}