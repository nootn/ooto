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
using System.Web.Mvc;
using DotNetAppStarterKit.Web.Security;
using OOTO.Core.DomainModel.Aggregate.UserAccountAggregate;
using OOTO.Core.DomainModel.Aggregate.UserAccountAggregate.Query;
using OOTO.Core.EventSourcing.Interface;

namespace OOTO.Web.Controllers
{
    public partial class HomeController : Controller
    {
        public const string ScenarioMachine1User1 = "Machine1User1";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<UserAccount> _userRepository;

        public HomeController(IUnitOfWork unitOfWork, IRepository<UserAccount> userRepository)
        {
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
        }

        public virtual ActionResult Index(string scenario = "")
        {
            switch (scenario)
            {
                case ScenarioMachine1User1:
                    using (_unitOfWork)
                    {
                        var existingUser = _userRepository.Query(new GetUserAccountByNameQuery("User1"));
                        if (existingUser == null)
                        {
                            existingUser = UserAccount.Create("User1");
                            _userRepository.Add(existingUser);
                        }
                        existingUser.LoginToMachine(DateTimeOffset.Now, "Machine1", "User1PC");

                        _unitOfWork.Commit();
                    }
                    break;
            }
            return View();
        }
    }
}