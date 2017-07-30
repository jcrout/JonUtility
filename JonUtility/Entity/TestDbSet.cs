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
    public class TestDbSet<TEntity> : DbSet<TEntity>, IQueryable, IEnumerable<TEntity>, IDbAsyncEnumerable<TEntity>
        where TEntity : class
    {
        // eventually, expand to deal with primary key order (using Index attribute) and also deal with cases where foreign keys act as primary keys
        private class ElementMetadata
        {
            public PropertyInfo[] KeyProperties { get; private set; }

            public ElementMetadata(Type elementType)
            {
                var properties = elementType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var query = (from p in properties
                             let attributes = p.GetCustomAttributes()
                             let keyAttribute = attributes.FirstOrDefault(a => typeof(KeyAttribute).IsAssignableFrom(a.GetType()))
                             where keyAttribute != null
                             select new { Property = p, Attributes = attributes, Key = (KeyAttribute)keyAttribute }).ToList();

                this.KeyProperties = query.Select(q => q.Property).ToArray();
                //query.Sort((a1, a2) => a1.Key.);
            }

            public bool IsMatch(TEntity entity, object[] keyValues)
            {
                if (keyValues == null || keyValues.Length != this.KeyProperties.Length)
                {
                    throw new ArgumentException(nameof(keyValues));
                }

                for (int i = 0; i < KeyProperties.Length; i++)
                {
                    var value = this.KeyProperties[i].GetValue(entity);
                    var type = value.GetType();

                    if (!object.Equals(value, keyValues[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

        }

        internal class TestDbAsyncQueryProvider<TEntity> : IDbAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal TestDbAsyncQueryProvider(IQueryProvider inner)
            {
                _inner = inner;
            }

            public IQueryable CreateQuery(Expression expression)
            {
                return new TestDbAsyncEnumerable<TEntity>(expression);
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                return new TestDbAsyncEnumerable<TElement>(expression);
            }

            public object Execute(Expression expression)
            {
                return _inner.Execute(expression);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return _inner.Execute<TResult>(expression);
            }

            public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute(expression));
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute<TResult>(expression));
            }
        }

        internal class TestDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
        {
            public TestDbAsyncEnumerable(IEnumerable<T> enumerable)
                : base(enumerable)
            { }

            public TestDbAsyncEnumerable(Expression expression)
                : base(expression)
            { }

            public IDbAsyncEnumerator<T> GetAsyncEnumerator()
            {
                return new TestDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
            }

            IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
            {
                return GetAsyncEnumerator();
            }

            IQueryProvider IQueryable.Provider
            {
                get { return new TestDbAsyncQueryProvider<T>(this); }
            }
        }

        internal class TestDbAsyncEnumerator<T> : IDbAsyncEnumerator<T>
        {
            private readonly IEnumerator<T> _inner;

            public TestDbAsyncEnumerator(IEnumerator<T> inner)
            {
                _inner = inner;
            }

            public void Dispose()
            {
                _inner.Dispose();
            }

            public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_inner.MoveNext());
            }

            public T Current
            {
                get { return _inner.Current; }
            }

            object IDbAsyncEnumerator.Current
            {
                get { return Current; }
            }
        }

        private ObservableCollection<TEntity> _data;
        private IQueryable _query;
        private ElementMetadata elementMetadata;

        public TestDbSet()
        {
            _data = new ObservableCollection<TEntity>();
            _query = _data.AsQueryable();
            elementMetadata = new ElementMetadata(typeof(TEntity));
        }

        public override TEntity Find(params object[] keyValues)
        {
            return this.Find(new CancellationToken(), keyValues);
        }

        public override Task<TEntity> FindAsync(params object[] keyValues)
        {
            return Task.FromResult<TEntity>(this.Find(new CancellationToken(), keyValues));
        }

        public override Task<TEntity> FindAsync(CancellationToken cancellationToken, params object[] keyValues)
        {
            return Task.FromResult<TEntity>(this.Find(cancellationToken, keyValues));
        }

        private TEntity Find(CancellationToken cancellationToken, params object[] keyValues)
        {
            if (keyValues == null || keyValues.Length != this.elementMetadata.KeyProperties.Length)
            {
                throw new ArgumentException(nameof(keyValues));
            }

            foreach (var entity in this._data)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (this.elementMetadata.IsMatch(entity, keyValues))
                {
                    return entity;
                }
            }

            return null;
        }


        public override TEntity Add(TEntity item)
        {
            _data.Add(item);
            return item;
        }

        public override TEntity Remove(TEntity item)
        {
            _data.Remove(item);
            return item;
        }

        public override TEntity Attach(TEntity item)
        {
            _data.Add(item);
            return item;
        }

        public override TEntity Create()
        {
            return Activator.CreateInstance<TEntity>();
        }

        public override TDerivedEntity Create<TDerivedEntity>()
        {
            return Activator.CreateInstance<TDerivedEntity>();
        }

        public override ObservableCollection<TEntity> Local
        {
            get { return _data; }
        }

        Type IQueryable.ElementType
        {
            get { return _query.ElementType; }
        }

        Expression IQueryable.Expression
        {
            get { return _query.Expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return new TestDbAsyncQueryProvider<TEntity>(_query.Provider); }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IDbAsyncEnumerator<TEntity> IDbAsyncEnumerable<TEntity>.GetAsyncEnumerator()
        {
            return new TestDbAsyncEnumerator<TEntity>(_data.GetEnumerator());
        }
    }

}
