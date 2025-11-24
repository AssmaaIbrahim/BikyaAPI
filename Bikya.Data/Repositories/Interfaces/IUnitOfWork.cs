using Microsoft.EntityFrameworkCore.Storage;

namespace Bikya.Data.Repositories.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for managing transactions and coordinating repositories.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Gets the repository for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <returns>The repository instance</returns>
        IGenericRepository<T> Repository<T>() where T : class;

        /// <summary>
        /// Begins a new transaction.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The transaction instance</returns>
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The number of state entries written to the database</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified action within a transaction.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the action</returns>
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the specified action within a transaction.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
    }
} 