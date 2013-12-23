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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Builder;
using Autofac.Integration.Mvc;
using AutofacContrib.DynamicProxy;
using NEventStore;
using NEventStore.Dispatcher;
using NEventStore.Persistence.SqlPersistence.SqlDialects;
using OOTO.Core.EventSourcing;
using OOTO.Core.EventSourcing.Domain.Interface;
using OOTO.Core.EventSourcing.Interface;
using OOTO.Web.Application.Interceptors;

namespace OOTO.Web
{
    public class MvcApplication : HttpApplication
    {
        public static AutofacDependencyResolver GlobalResolver { get; private set; }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConfigureIoc();
        }

        private static void ConfigureIoc()
        {
            var builder = new ContainerBuilder();

            //Web components
            var webAssembly = typeof (MvcApplication).Assembly;
            builder.RegisterControllers(webAssembly);
            builder.RegisterFilterProvider();
            builder.RegisterModule(new AutofacWebTypesModule());

            //Components defined within this website
            builder.RegisterType<MiniProfilerInterceptor>().AsSelf();


            //Core components
            var coreAssembly = typeof (Repository<>).Assembly;
            builder.RegisterGeneric(typeof (Repository<>))
                .As(typeof (IRepository<>))
                .InstancePerDependency()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.RegisterType<UnitOfWork>()
                .AsImplementedInterfaces()
                .OnRelease(u =>
                {
                    u.Commit();
                    u.Dispose();
                })
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.RegisterType<QueryableSnapshot>()
                .AsImplementedInterfaces()
                .As<IHandle<IFact>>()
                .SingleInstance()
                .AutoActivate()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.RegisterType<AggregateBuilder>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.RegisterType<DomainEventBroker>()
                .AsImplementedInterfaces()
                .SingleInstance()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.Register(c => Thread.CurrentPrincipal.Identity)
                .As<IIdentity>()
                .InstancePerDependency();

            builder.RegisterAssemblyTypes(coreAssembly)
                .Where(t => t.IsClosedTypeOf(typeof (IHandleDuringUnitOfWork<>)))
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof (MiniProfilerInterceptor));

            builder.RegisterType<SystemClock>()
                .AsImplementedInterfaces()
                .SingleInstance();

            //    Sample wireup of NEventStore using RavenDb.
            //    builder.Register(c =>
            //    {
            //        var bus = c.Resolve<IDomainEventBroker>();
            //        var factStore =
            //               Wireup.Init()
            //                     .LogToOutputWindow()
            //                     .UsingRavenPersistence(c.Resolve<IDocumentStore>)
            //                     .InitializeStorageEngine()
            //                     .UsingJsonSerialization()
            //                     .UsingSynchronousDispatchScheduler()
            //                     .DispatchTo(bus)
            //                     .Build();
            //        bus.StartDispatching();
            //        return factStore;
            //    })
            //    .As<IStoreEvents>()
            //    .SingleInstance();

            //TODO: delete this!  Just testing
            const string connectionString =
                        @"Data Source=(LocalDb)\Projects;Initial Catalog=master;Integrated Security=True;";
            const string databaseName = "TestSqlPersistence";
            using (var conn = new SqlConnection(connectionString))
            {
                using (
                    var cmd =
                        new SqlCommand(
                            string.Format(
                                "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{0}') CREATE DATABASE {0}",
                                databaseName), conn))
                {
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            builder.Register(c =>
            {
                //var bus = c.Resolve<IDomainEventBroker>();
                var factStore =
                       Wireup.Init()
                             .UsingSqlPersistence("TestSqlPersistence")
                             .WithDialect(new MsSqlDialect())
                             .EnlistInAmbientTransaction() // two-phase commit
                             //.UsingInMemoryPersistence()
                             .InitializeStorageEngine()
                             .UsingBinarySerialization()
                             .UsingSynchronousDispatchScheduler()
                             .DispatchTo(new DelegateMessageDispatcher(DispatchCommit))
                             .Build();
                //bus.StartDispatching();
                return factStore;
            })
            .As<IStoreEvents>()
            .SingleInstance();

            //Example read model..
            //builder.RegisterType<ToDoReadModelReadModel>()
            //    .AsImplementedInterfaces()
            //    .AsSelf()
            //    .SingleInstance()
            //    .AutoActivate();


            //Build and set resolver
            try
            {
                var container = builder.Build();

                GlobalResolver = new AutofacDependencyResolver(container);

                DependencyResolver.SetResolver(GlobalResolver);
            }
            catch (Exception ex)
            {
                if (ex is ReflectionTypeLoadException)
                {
                    var typeLoadException = ex as ReflectionTypeLoadException;
                    var loaderExceptions = typeLoadException.LoaderExceptions;
                    throw new AggregateException(typeLoadException.Message, loaderExceptions);
                }
                throw;
            }
        }

        private static void DispatchCommit(Commit commit)
        {
            Debug.WriteLine("Dispatch Commit: {0}", commit.CommitId);
            // This is where we'd hook into our messaging infrastructure, such as NServiceBus,
            // MassTransit, WCF, or some other communications infrastructure.
            // This can be a class as well--just implement IDispatchCommits.
            //try
            //{
            //    foreach (var @event in commit.Events)
            //        Console.WriteLine(Resources.MessagesDispatched + ((SomeDomainEvent)@event.Body).Value);
            //}
            //catch (Exception)
            //{
            //    Console.WriteLine(Resources.UnableToDispatch);
            //}
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            if (interfaceTypes.Where(it => it.IsGenericType).Any(it => it.GetGenericTypeDefinition() == genericType))
                return true;

            var baseType = givenType.BaseType;
            if (baseType == null) return false;

            return baseType.IsGenericType &&
                   baseType.GetGenericTypeDefinition() == genericType ||
                   IsAssignableToGenericType(baseType, genericType);
        }

        private static List<IRegistrationBuilder<object, object, object>> RegisterGenericTypes(ContainerBuilder builder,
            Assembly parentAssembly,
            Type
                genericRepositoryType,
            bool asInterfaces)
        {
            var types = parentAssembly.GetExportedTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Where(t => IsAssignableToGenericType(t, genericRepositoryType))
                .ToArray();

            var res = new List<IRegistrationBuilder<object, object, object>>();
            foreach (var type in types)
            {
                if (type.IsGenericType)
                    res.Add(asInterfaces
                        ? builder.RegisterGeneric(type).AsImplementedInterfaces()
                        : builder.RegisterGeneric(type).AsSelf());
                else
                    res.Add(asInterfaces
                        ? builder.RegisterType(type).AsImplementedInterfaces()
                        : builder.RegisterType(type).AsSelf());
            }
            return res;
        }
    }
}