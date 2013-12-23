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
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using OOTO.Core.EventSourcing.Domain.Interface;
using OOTO.Core.EventSourcing.Interface;
using OOTO.Core.Extensions;

namespace OOTO.Core.EventSourcing
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    public class UnitOfWork : IUnitOfWork
    {
        //private readonly IStoreEvents _factStore;
        private readonly IClock _clock;
        private readonly IDomainEventBroker _domainEventBroker;
        private readonly List<IAggregateRoot> _enlistedAggregateRoots = new List<IAggregateRoot>();
        private readonly IIdentity _identity;
        private readonly Guid _unitOfWorkId = Guid.NewGuid();
        private int _committed;

        public UnitOfWork(
            //IStoreEvents factStore, 
            IClock clock,
            IIdentity identity,
            IDomainEventBroker domainEventBroker)
        {
            //_factStore = factStore;

            _clock = clock;
            _identity = identity;
            _domainEventBroker = domainEventBroker;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void EnlistAggregate(IAggregateRoot item)
        {
            _enlistedAggregateRoots.Add(item);
        }

        public T TryGetEnlistedAggregateRoot<T>(Guid id) where T : IAggregateRoot
        {
            return _enlistedAggregateRoots.OfType<T>().FirstOrDefault(ar => ar.Id == id);
        }

        public IQueryable<T> EnlistedAggregateRoots<T>() where T : IAggregateRoot
        {
            return _enlistedAggregateRoots.OfType<T>().AsQueryable();
        }

        public void Commit()
        {
            if (Interlocked.CompareExchange(ref _committed, 1, 0) != 0) return;

            var allFactsForThisUnitOfWork = GetAllFactsForThisUnitOfWork();
            SetAllFactDetails(allFactsForThisUnitOfWork);
            DispatchAllFacts(allFactsForThisUnitOfWork);

            //CommitAllFacts(allFactsForThisUnitOfWork);
        }

        private List<IFact> GetAllFactsForThisUnitOfWork()
        {
            var allFactsForThisUnitOfWork = new List<IFact>();

            while (true)
            {
                var factsFromThisPass = _enlistedAggregateRoots
                    .SelectMany(ar => ar.GetPendingFacts())
                    .ToArray();

                if (factsFromThisPass.None()) break;

                allFactsForThisUnitOfWork.AddRange(factsFromThisPass);

                foreach (var fact in factsFromThisPass)
                {
                    _domainEventBroker.RaiseWithinUnitOfWork(fact, this);
                }
            }

            return allFactsForThisUnitOfWork;
        }

        private void SetAllFactDetails(IEnumerable<IFact> allFactsForThisUnitOfWork)
        {
            var userId = _identity.IsAuthenticated ? _identity.Name : "[UNKNOWN]";
            var timestamp = _clock.UtcNow;
            var sequenceNumber = 0;

            foreach (var fact in allFactsForThisUnitOfWork)
            {
                fact.SetUnitOfWorkDetails(_unitOfWorkId, sequenceNumber++, timestamp, userId);
            }
        }

        private void DispatchAllFacts(List<IFact> allFactsForThisUnitOfWork)
        {
            allFactsForThisUnitOfWork.ForEach(f => _domainEventBroker.Raise(f));
        }

        private void CommitAllFacts(IEnumerable<IFact> allFactsForThisUnitOfWork)
        {
            throw new NotImplementedException(
                "Add NEventStore and uncomment this, and remove DispatchAllFacts as NEventStore handles dispatch after it commits.");

            //var groupedFacts = allFactsForThisUnitOfWork.GroupBy(f => f.AggregateRootId);

            //foreach (var factGroup in groupedFacts)
            //{

            //    var aggregateRootId = factGroup.First().AggregateRootId;

            //    using (var factStream = _factStore.OpenStream(aggregateRootId, 0, int.MaxValue))
            //    {
            //        foreach (var fact in factGroup)
            //        {
            //            factStream.Add(new EventMessage { Body = fact });
            //        }

            //        factStream.CommitChanges(Guid.NewGuid());
            //    }
            //}
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }
    }
}