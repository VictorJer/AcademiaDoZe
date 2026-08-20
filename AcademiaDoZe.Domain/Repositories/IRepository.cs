using AcademiaDoZe.Domain.Common; // Victor Jeremias 
using AcademiaDoZe.Domain.Entities;
namespace AcademiaDoZe.Domain.Repositories;
// Interface genérica para repositórios. Restrita apenas a Raízes de Agregado (Aggregate Roots) no DDD.// Define os contratos essenciais para a persistência de dados.
// Herda de EntidadeBase para garantir que TEntidadeBase seja uma entidade válida, e seu uso somente no domain.
// Métodos assíncronos (Task), alinhados com práticas modernas de acesso a dados.
public interface IRepository<TEntidadeBase> where TEntidadeBase : EntidadeBase, IAggregateRoot
{
    Task<TEntidadeBase?> ObterPorId(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntidadeBase>> ObterTodos(CancellationToken cancellationToken = default);
    Task<TEntidadeBase> Adicionar(TEntidadeBase EntidadeBase, CancellationToken cancellationToken = default);
    Task<TEntidadeBase> Atualizar(TEntidadeBase EntidadeBase, CancellationToken cancellationToken = default);
    Task<bool> Remover(int id, CancellationToken cancellationToken = default);
}