using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace JonUtility.Entity
{
    public abstract class TestDbContext : IDatabaseContext
    {
        public int SaveChangesCount { get; private set; }

        private DbSetProperty[] dbSetProperties;

        public void Dispose()
        {
        }

        public int SaveChanges()
        {
            this.SaveChangesCount++;
            return 1;
        }

        public Task<int> SaveChangesAsync()
        {
            this.SaveChangesCount++;
            return Task.FromResult(1);
        }

        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            var properties = this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var set = (from p in this.dbSetProperties
                       where p.ElementType == typeof(TEntity)
                       select p).FirstOrDefault();

            return set != null ? (DbSet<TEntity>)set.Property.GetValue(this) : null;
        }

        public TestDbContext()
        {
            this.dbSetProperties = (from p in this.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                                    let pType = p.PropertyType
                                    let genericArguments = pType.GetGenericArguments()
                                    where genericArguments.Length == 1
                                    where typeof(DbSet<>).MakeGenericType(genericArguments[0]).IsAssignableFrom(pType)
                                    select new DbSetProperty() { Property = p, ElementType = genericArguments[0], TestDbSetType = typeof(TestDbSet<>).MakeGenericType(genericArguments[0]) }).ToArray();

            foreach (var dbSetProperty in this.dbSetProperties)
            {
                dbSetProperty.Property.SetValue(this, Activator.CreateInstance(dbSetProperty.TestDbSetType));
            }
        }

        private class DbSetProperty
        {
            public PropertyInfo Property { get; set; }
            public Type ElementType { get; set; }
            public Type TestDbSetType { get; set; }
        }
    }
}
