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
        public void getInstanceOf ()
        {
            var instance = ModuleContainer.Instance.GetInstanceOf<IModuleTeste> ();

            Assert.NotNull (instance);

            Assert.NotEmpty (instance.SayHello ());
        }

        [Fact]
        public void getInstanceAs ()
        {
            var instance = ModuleContainer.Instance.GetInstanceAs<IModuleTeste> ("SimpleHelpersTests.Test1");

            Assert.NotNull (instance);

            Assert.NotEmpty (instance.SayHello ());
        }
    }

    public interface IModuleTeste
    {
        string SayHello ();
    }

    public class Test1 : IModuleTeste
    {
        public string SayHello()
        {
            return "Hello Test1";
        }
    }
}
