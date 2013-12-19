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
using System.Linq;
using OOTO.Core.EventSourcing.Domain;
using OOTO.Core.EventSourcing.Domain.Interface;
using OOTO.Core.EventSourcing.Interface;

namespace OOTO.Core.EventSourcing
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public class Repository<T> : IRepository<T> where T : class, IAggregateRoot
    {
        private readonly IQueryableSnapshot _queryableSnapshot;
        private readonly IUnitOfWork _unitOfWork;

        public Repository(IQueryableSnapshot queryableSnapshot, IUnitOfWork unitOfWork)
        {
            _queryableSnapshot = queryableSnapshot;
            _unitOfWork = unitOfWork;
        }

        public T GetById(Guid id)
        {
            var aggregateRoot = _queryableSnapshot.GetById<T>(id);

            var clone = aggregateRoot.BinaryClone();

            _unitOfWork.EnlistAggregate(clone);
            return clone;
        }

        public void Add(T item)
        {
            _unitOfWork.EnlistAggregate(item);
        }

        public T[] Query(IQuery<T> query)
        {
            return ExecuteQuery(query).ToArray();
        }

        public TOut Query<TOut>(IQuery<T, TOut> query)
        {
            return ExecuteQuery(query);
        }

        public int Count(IQuery<T> query)
        {
            return ExecuteQuery(query).Count();
        }

        private IQueryable<T> ExecuteQuery(IQuery<T> query)
        {
            var merged = _unitOfWork.EnlistedAggregateRoots<T>()
                .Union(_queryableSnapshot.Items<T>(), new AggregateRootIdEqualityComparer<T>());

            return query.Execute(merged)
                .Select(storedEntity => FindEnlistedEntityOrCloneSnapshottedEntity(storedEntity));
        }

        private TOut ExecuteQuery<TOut>(IQuery<T, TOut> query)
        {
            //TODO: this _doesn't_ enlist in the unitofwork if it wasn't already there, which isn't consistent.
            var merged = _unitOfWork.EnlistedAggregateRoots<T>()
                .Union(_queryableSnapshot.Items<T>(), new AggregateRootIdEqualityComparer<T>());
            return query.Execute(merged);
        }

        private T FindEnlistedEntityOrCloneSnapshottedEntity(T storedEntity)
        {
            // is this entity already participating in our current transaction? return that if so
            var entityInTransaction = _unitOfWork.TryGetEnlistedAggregateRoot<T>(storedEntity.Id);
            if (entityInTransaction != null) return entityInTransaction;

            // otherwise do a binary clone and enlist the clone
            var clone = storedEntity.BinaryClone();
            _unitOfWork.EnlistAggregate(clone);
            return clone;
        }
    }
}