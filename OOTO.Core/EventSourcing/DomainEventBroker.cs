// /*
// Copyright (c) 2013 Andrew Newton (http://www.nootn.com.au)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */

using System.Collections;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using OOTO.Core.EventSourcing.Domain.Interface;
using OOTO.Core.EventSourcing.Interface;

namespace OOTO.Core.EventSourcing
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    internal class DomainEventBroker : IDomainEventBroker
    {
        private readonly ILifetimeScope _lifetimeScope;

        public DomainEventBroker(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public void Raise(IFact fact)
        {
            RaiseForBaseHandlers(fact);

            var handlerType = typeof (IHandle<>).MakeGenericType(fact.GetType());
            var handlersType = typeof (IEnumerable<>).MakeGenericType(handlerType);
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var handlers = (IEnumerable) scope.Resolve(handlersType);

                foreach (var handler in handlers)
                    ((dynamic) handler).Handle((dynamic) fact);
            }
        }


        public void RaiseWithinUnitOfWork(IFact fact, IUnitOfWork unitOfWork)
        {
            var handlerType = typeof (IHandleDuringUnitOfWork<>).MakeGenericType(fact.GetType());
            var handlersType = typeof (IEnumerable<>).MakeGenericType(handlerType);
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var handlers =
                    (IEnumerable)
                        scope.Resolve(handlersType,
                            new Parameter[] {new TypedParameter(typeof (IUnitOfWork), unitOfWork)});
                foreach (var handler in handlers)
                    ((dynamic) handler).Handle((dynamic) fact);
            }
        }

        private void RaiseForBaseHandlers(IFact fact)
        {
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var handlers = (IEnumerable) scope.Resolve(typeof (IEnumerable<IHandle<IFact>>));

                foreach (var handler in handlers)
                    ((dynamic) handler).Handle((dynamic) fact);
            }
        }
    }
}