using System;
using System.Data.Entity;
using System.Threading.Tasks;

namespace JonUtility.Entity
{
    public interface IDatabaseContext : IDisposable
    {
        DbSet<TEntity> Set<TEntity>() where TEntity : class;
        int SaveChanges();
        Task<int> SaveChangesAsync();
    }
}
