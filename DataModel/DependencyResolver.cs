using System.ComponentModel.Composition;
using System.Data.Entity;
using DataModel.UnitOfWork;
using Resolver;

namespace DataModel
{
    [Export(typeof(IComponent))]
    public class DependencyResolver : IComponent
    {
        public void SetUp(IRegisterComponent registerComponent)
        {
            registerComponent.RegisterType<IUnitOfWork,UnitOfWork.UnitOfWork>();
        }
    }
}
