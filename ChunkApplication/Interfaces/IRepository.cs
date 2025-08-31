namespace ChunkApplication.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its identifier
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>Entity or null if not found</returns>
    Task<T?> GetByIdAsync(string id);
    
    /// <summary>
    /// Gets all entities
    /// </summary>
    /// <returns>Collection of entities</returns>
    Task<IEnumerable<T>> GetAllAsync();
    
    /// <summary>
    /// Adds a new entity
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task AddAsync(T entity);
    
    /// <summary>
    /// Updates an existing entity
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task UpdateAsync(T entity);
    
    /// <summary>
    /// Deletes an entity by its identifier
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>Task representing the asynchronous operation</returns>
    Task DeleteAsync(string id);
    
    /// <summary>
    /// Saves all changes to the database
    /// </summary>
    /// <returns>Number of affected rows</returns>
    Task<int> SaveChangesAsync();
}
