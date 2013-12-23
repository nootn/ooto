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
using OOTO.Core.DomainModel.Aggregate.UserAccountAggregate.Fact;
using OOTO.Core.EventSourcing.Domain;
using OOTO.Core.EventSourcing.Domain.Interface;

namespace OOTO.Core.DomainModel.Aggregate.UserAccountAggregate
{
    [Serializable]
    public class MachineLogin : Entity
    {
        internal MachineLogin(IAppendFacts parent) : base(parent)
        {
        }

        public DateTimeOffset LoggedInAt { get; private set; }

        public string MachineName { get; private set; }

        public string ClientMachineName { get; private set; }

        internal static MachineLogin Create(DateTimeOffset loggedInAt, string machineName, string clientMachineName, IAppendFacts parent)
        {
            var fact = new MachineLoginOccurredFact
            {
                Id = Guid.NewGuid(),
                ClientMachineName = clientMachineName,
                LoggedInAt = loggedInAt,
                MachineName = machineName
            };

            var machineLogin = new MachineLogin(parent);
            machineLogin.Append(fact);
            machineLogin.Apply(fact);

            return machineLogin;
        }

        internal static MachineLogin HydrateFrom(MachineLoginOccurredFact fact, UserAccount parent)
        {
            var machineLogin = new MachineLogin(parent);
            machineLogin.Apply(fact);
            return machineLogin;
        }

        public void Apply(MachineLoginOccurredFact fact)
        {
            Id = fact.Id;
            ClientMachineName = fact.ClientMachineName;
            LoggedInAt = fact.LoggedInAt;
            MachineName = fact.MachineName;
        }
    }
}