using System.Web.Http;
//using DataModel.UnitOfWork;
using Microsoft.Practices.Unity;
using Resolver;
using Unity.Mvc3;

namespace WebApi
{
    public static class Bootstrapper
    {
        public static void  Initialise()
        {
            var container = BuildUnityContainer();

            System.Web.Mvc.DependencyResolver.SetResolver(new UnityDependencyResolver(container));

            // register dependency resolver for WebAPI RC
            GlobalConfiguration.Configuration.DependencyResolver = new Unity.WebApi.UnityDependencyResolver(container);
        }

        private static IUnityContainer BuildUnityContainer()
        {
            var container = new UnityContainer();
            RegisterTypes(container);
            return container;
        }

        public static void RegisterTypes(IUnityContainer container)
        {
            //Component initialization via MEF
            ComponentLoader.LoadContainer(container, ".\\bin", "WebApi.dll");
            ComponentLoader.LoadContainer(container, ".\\bin", "BusinessServices.dll");
            ComponentLoader.LoadContainer(container, ".\\bin", "DataModel.dll");
        }
    }
}