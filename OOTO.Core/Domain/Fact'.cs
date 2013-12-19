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
using OOTO.Core.Domain.Interface;

namespace OOTO.Core.Domain
{
    //Originally from https://github.com/andrewabest/EventSourcing101
    [Serializable]
    public abstract class Fact<T> : IFact
        where T : IAggregateRoot
    {
        protected Fact()
        {
            FactId = Guid.NewGuid();
        }

        public Guid AggregateRootId { get; set; }
        public Guid UnitOfWorkId { get; set; }
        public int UnitOfWorkSequenceNumber { get; set; }

        public Guid Id { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public Guid? CreatedBy { get; set; }

        public Guid FactId { get; set; }


        public void SetUnitOfWorkDetails(Guid unitOfWorkId, int unitOfWorkSequenceNumber, DateTimeOffset timestamp,
            Guid? userId)
        {
            if (unitOfWorkId == Guid.Empty)
                throw new ArgumentException("unitOfWorkId", "An ID for the current unit of work must be provided!");

            UnitOfWorkId = unitOfWorkId;
            UnitOfWorkSequenceNumber = unitOfWorkSequenceNumber;
            Timestamp = timestamp;
            CreatedBy = userId;
        }

        public string AggregateRootTypeName
        {
            get { return typeof (T).FullName; }
        }
    }
}