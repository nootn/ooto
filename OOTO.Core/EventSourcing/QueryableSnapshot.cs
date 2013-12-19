// /*
// Copyright (c) 2013 Andrew Newton (http://www.nootn.com.au)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */

using System;
using System.Collections.Concurrent;
using System.Linq;
using OOTO.Core.EventSourcing.Domain.Interface;
using OOTO.Core.EventSourcing.Interface;

namespace OOTO.Core.EventSourcing
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public class QueryableSnapshot : IHandle<IFact>, IQueryableSnapshot
    {
        private readonly IAggregateBuilder _builder;

        private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, IAggregateRoot>> _items =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Guid, IAggregateRoot>>();

        public QueryableSnapshot(IAggregateBuilder builder)
        {
            _builder = builder;
        }

        public void Handle(IFact fact)
        {
            var entityType = Type.GetType(fact.AggregateRootTypeName);

            var aggregateDictionary = _items.GetOrAdd(
                entityType,
                t => new ConcurrentDictionary<Guid, IAggregateRoot>());

            var aggregate = aggregateDictionary.GetOrAdd(
                fact.AggregateRootId,
                t => (IAggregateRoot) Activator.CreateInstance(entityType, true)
                );

            Apply(entityType, aggregate, fact);
        }

        public T GetById<T>(Guid id) where T : IAggregateRoot
        {
            ConcurrentDictionary<Guid, IAggregateRoot> aggregateDictionary;
            if (_items.TryGetValue(typeof (T), out aggregateDictionary))
            {
                IAggregateRoot aggregateRoot;
                if (aggregateDictionary.TryGetValue(id, out aggregateRoot))
                {
                    return (T) aggregateRoot;
                }
            }

            return default(T);
        }

        public IQueryable<T> Items<T>() where T : IAggregateRoot
        {
            ConcurrentDictionary<Guid, IAggregateRoot> aggregateDictionary;
            if (_items.TryGetValue(typeof (T), out aggregateDictionary))
            {
                return aggregateDictionary.Values.Cast<T>().AsQueryable();
            }

            return Enumerable.Empty<T>().AsQueryable();
        }

        private void Apply(Type aggregateType, IAggregateRoot aggregateRoot, IFact fact)
        {
            var openGenericMethod = typeof (IAggregateBuilder).GetMethod("Apply");
            var closedGenericMethod = openGenericMethod.MakeGenericMethod(aggregateType);
            closedGenericMethod.Invoke(_builder, new object[] {aggregateRoot, fact});
        }
    }
}