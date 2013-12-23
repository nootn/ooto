// /*
// Copyright (c) 2013 Andrew Newton (http://www.nootn.com.au)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */

using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using OOTO.Web.App_Start;
using StackExchange.Profiling;
using StackExchange.Profiling.MVCHelpers;
using StackExchange.Profiling.SqlFormatters;
using WebActivator;

//using System.Data;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
//using StackExchange.Profiling.Data.EntityFramework;
//using StackExchange.Profiling.Data.Linq2Sql;

[assembly: WebActivator.PreApplicationStartMethod(
    typeof (MiniProfilerPackage), "PreStart")]
[assembly: PostApplicationStartMethod(
    typeof (MiniProfilerPackage), "PostStart")]

namespace OOTO.Web.App_Start
{
    public static class MiniProfilerPackage
    {
        public static void PreStart()
        {
            // Be sure to restart you ASP.NET Developement server, this code will not run until you do that. 

            //Non SQL Server based installs can use other formatters like: new StackExchange.Profiling.SqlFormatters.InlineFormatter()
            MiniProfiler.Settings.SqlFormatter = new SqlServerFormatter();

            //To profile a standard DbConnection: 
            // var profiled = new ProfiledDbConnection(cnn, MiniProfiler.Current);

            //If you are profiling EF code first try: 
            // MiniProfilerEF.Initialize();

            //Make sure the MiniProfiler handles BeginRequest and EndRequest
            DynamicModuleUtility.RegisterModule(typeof (MiniProfilerStartupModule));

            //Setup profiler for Controllers via a Global ActionFilter
            GlobalFilters.Filters.Add(new ProfilingActionFilter());

            // You can use this to check if a request is allowed to view results
            //MiniProfiler.Settings.Results_Authorize = (request) =>
            //{
            // you should implement this if you need to restrict visibility of profiling on a per request basis 
            //    return !DisableProfilingResults; 
            //};

            // the list of all sessions in the store is restricted by default, you must return true to alllow it
            //MiniProfiler.Settings.Results_List_Authorize = (request) =>
            //{
            // you may implement this if you need to restrict visibility of profiling lists on a per request basis 
            //return true; // all requests are kosher
            //};
        }

        public static void PostStart()
        {
            // Intercept ViewEngines to profile all partial views and regular views.
            // If you prefer to insert your profiling blocks manually you can comment this out
            var copy = ViewEngines.Engines.ToList();
            ViewEngines.Engines.Clear();
            foreach (var item in copy)
            {
                ViewEngines.Engines.Add(new ProfilingViewEngine(item));
            }
        }
    }

    public class MiniProfilerStartupModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += (sender, e) =>
            {
                var request = ((HttpApplication) sender).Request;
                if (request.IsLocal)
                {
                    MiniProfiler.Start();
                }
            };


            /*
            context.AuthenticateRequest += (sender, e) =>
            {
                if (!CurrentUserIsAllowedToSeeProfiler())
                {
                    StackExchange.Profiling.MiniProfiler.Stop(discardResults: true);
                }
            };
            */

            context.EndRequest += (sender, e) => MiniProfiler.Stop();
        }

        public void Dispose()
        {
        }
    }
}