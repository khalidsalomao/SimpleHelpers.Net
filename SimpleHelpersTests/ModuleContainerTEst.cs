using SimpleHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SimpleHelpersTests
{
    public class ModuleContainerTest
    {
        [Fact]
        public void getInstance_with_type_name ()
        {
            object instance = ModuleContainer.Instance.GetInstance ("SimpleHelpersTests.Test1");

            Assert.NotNull (instance);

            Assert.NotEmpty (((ITestModule)instance).SayHello ());
        }

        [Fact]
        public void getInstance_with_type ()
        {
            object instance = ModuleContainer.Instance.GetInstance (typeof (Test1));

            Assert.NotNull (instance);

            Assert.NotEmpty (((ITestModule)instance).SayHello ());
        }

        [Fact]
        public void getInstance_with_interface_will_not_work ()
        {
            object instance = ModuleContainer.Instance.GetInstance (typeof (ITestModule));

            Assert.Null (instance);
        }

        [Fact]
        public void getTypes ()
        {
            var list = ModuleContainer.Instance.GetTypesOf ("SimpleHelpersTests.Test1").ToList ();

            Assert.NotEmpty (list);
        }

        
        [Fact]
        public void getInstanceOf ()
        {
            var instance = ModuleContainer.Instance.GetInstanceOf<ITestModule> ();

            Assert.NotNull (instance);

            Assert.NotEmpty (instance.SayHello ());
        }

        [Fact]
        public void getInstanceAs ()
        {
            var instance = ModuleContainer.Instance.GetInstanceAs<ITestModule> ("SimpleHelpersTests.Test1");

            Assert.NotNull (instance);

            Assert.NotEmpty (instance.SayHello ());
        }
    }

    public interface ITestModule
    {
        string SayHello ();
    }

    public class Test1 : ITestModule
    {
        public string SayHello()
        {
            return "Hello Test1";
        }
    }
}
