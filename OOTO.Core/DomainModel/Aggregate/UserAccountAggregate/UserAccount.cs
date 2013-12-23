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
using System.Collections.ObjectModel;
using OOTO.Core.DomainModel.Aggregate.UserAccountAggregate.Fact;
using OOTO.Core.EventSourcing.Domain;

namespace OOTO.Core.DomainModel.Aggregate.UserAccountAggregate
{
    [Serializable]
    public class UserAccount : AggregateRoot
    {
        private readonly ICollection<MachineLogin> _machineLogins;

        private UserAccount()
        {
            _machineLogins = new Collection<MachineLogin>();
        }

        public string Name { get; private set; }

        public IEnumerable<MachineLogin> MachineLogins
        {
            get { return _machineLogins; }
        }

        public static UserAccount Create(string name)
        {
            var fact = new UserCreatedFact {Name = name, AggregateRootId = Guid.NewGuid()};

            var userAccount = new UserAccount();
            userAccount.Append(fact);
            userAccount.Apply(fact);

            return userAccount;
        }

        public void Apply(UserCreatedFact fact)
        {
            Id = fact.AggregateRootId;
            Name = fact.Name;
        }

        public void Apply(MachineLoginOccurredFact fact)
        {
            var machineLogin = MachineLogin.HydrateFrom(fact, this);
            _machineLogins.Add(machineLogin);
        }

        public MachineLogin LoginToMachine(DateTimeOffset loggedInAt, string machineName, string clientMachineName)
        {
            var machineLogin = MachineLogin.Create(loggedInAt, machineName, clientMachineName, this);
            _machineLogins.Add(machineLogin);
            return machineLogin;
        }
    }
}