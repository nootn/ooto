﻿// /*
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
using System.Collections.Generic;
using OOTO.Core.Domain.Interface;

namespace OOTO.Core.Domain
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    [Serializable]
    public abstract class AggregateRoot : IAggregateRoot, IAppendFacts
    {
        private readonly ConcurrentQueue<IFact> _pendingFacts = new ConcurrentQueue<IFact>();

        public Guid Id { get; protected set; }

        public IEnumerable<IFact> GetPendingFacts()
        {
            IFact fact;

            while (_pendingFacts.TryDequeue(out fact)) yield return fact;
        }

        public void Append(IFact fact)
        {
            if (fact.AggregateRootId == Guid.Empty) fact.AggregateRootId = Id;

            _pendingFacts.Enqueue(fact);
        }
    }
}