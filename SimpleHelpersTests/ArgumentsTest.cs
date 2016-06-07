using SimpleHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace SimpleHelpersTests
{
    public class ArgumentsTest
    {
        [Fact]
        public void ParseCommandLineArguments ()
        {
            var instance = new Arguments ();

            var parsed = instance.ParseCommandLineArguments ("-h -k1 v1 -k2 v2 v2.2".Split ());
            Assert.NotNull (parsed);

            Assert.True (parsed.Get ("h", false));
            Assert.True (parsed.Get ("k1") == "v1");
            Assert.True (parsed.Get ("k2") == "v2.2");
        }

    }

    public class FlexibleOptionsTest
    {
        [Fact]
        public void Get_CaseInsensitive_ShouldWork ()
        {
            var expected = "v1";

            var opt1 = new FlexibleOptions ();
            opt1.Set ("key1", expected);

            Assert.Equal (expected, opt1.Get ("KEY1"));
        }

        [Fact]
        public void Get_AsDateTime_ShouldWork ()
        {
            // datetime without milliseconds
            var expected = new DateTime (2015, 01, 02, 03, 04, 05);

            var opt1 = new FlexibleOptions ();

            opt1.Set ("dtAsDateTime", expected);
            Assert.Equal (expected, opt1.Get<DateTime> ("dtAsDateTime", DateTime.MinValue));

            opt1.Set ("dtAsString", expected.ToString (System.Globalization.CultureInfo.InvariantCulture));
            Assert.Equal (expected, opt1.Get<DateTime> ("dtAsString", DateTime.MinValue));

            opt1.Set ("dtAsISOString", expected.ToString ("o"));
            Assert.Equal (expected, opt1.Get<DateTime> ("dtAsISOString", DateTime.MinValue));

            opt1.Set ("dtAsDate", expected.ToString ("yyyyMMdd"));
            Assert.Equal (expected.Date, opt1.Get<DateTime> ("dtAsDate", DateTime.MinValue));
        }

        [Fact]
        public void SetAlias_AllowGet_ShouldWork()
        {
            var expected = "v3";

            var opt1 = new FlexibleOptions ();
            opt1.Set ("k3", expected);
            
            opt1.SetAlias ("k3", "3", "key3");

            Assert.Equal (expected, opt1.Get ("k3"));
            Assert.Equal (expected, opt1.Get ("key3"));
            Assert.Equal (expected, opt1.Get ("3"));
            Assert.True (String.IsNullOrEmpty (opt1.Get ("KEY")));
        }

        [Fact]
        public void SetAlias_AllowCaseInsensitiveGet_ShouldWork ()
        {
            var expected = "v3";

            var opt1 = new FlexibleOptions ();
            opt1.Set ("k3", expected);

            opt1.SetAlias ("k3", "3", "key3");

            Assert.Equal (expected, opt1.Get ("K3"));
            Assert.Equal (expected, opt1.Get ("KEY3"));            
        }

        [Fact]
        public void Merge_WithComplexPriority_ShouldWork ()
        {
            var opt1 = new FlexibleOptions ();
            opt1.Set ("k1", "v1");
            opt1.Set ("k2", "v2");
            opt1.Set ("k3", "v3");

            opt1.SetAlias ("k3", "3", "key3");

            var opt2 = new FlexibleOptions ();
            opt2.Set ("k2", "v2.2");

            var opt3 = new FlexibleOptions ();
            opt3.Set ("k3", "v3.3");

            // opt1 has lower priority
            var merge = FlexibleOptions.Merge (opt1, opt2, opt3);
            Assert.NotNull (merge);
            Assert.Equal (opt1.Get ("k1"), merge.Get ("k1"));
            Assert.Equal (opt2.Get ("k2"), merge.Get ("k2"));
            Assert.Equal (opt3.Get ("k3"), merge.Get ("k3"));
            // alias
            Assert.Equal (opt3.Get ("k3"), merge.Get ("key3"));
            Assert.Equal (opt3.Get ("k3"), merge.Get ("3"));

            // opt1 has higher priority
            merge = FlexibleOptions.Merge (opt2, opt3, opt1);
            Assert.NotNull (merge);
            Assert.Equal (opt1.Get ("k1"), merge.Get ("k1"));
            Assert.NotEqual (opt2.Get ("k2"), merge.Get ("k2"));
            Assert.NotEqual (opt3.Get ("k3"), merge.Get ("k3"));
            Assert.Equal (opt1.Get ("k2"), merge.Get ("k2"));
            Assert.Equal (opt1.Get ("k3"), merge.Get ("k3"));
            // alias
            Assert.Equal (opt1.Get ("k3"), merge.Get ("key3"));
            Assert.Equal (opt1.Get ("k3"), merge.Get ("3"));

            // opt1 has higher priority but with null options
            merge = FlexibleOptions.Merge (null, opt2, null, opt3, opt1, null);
            Assert.NotNull (merge);
            Assert.Equal (opt1.Get ("k1"), merge.Get ("k1"));
            Assert.NotEqual (opt2.Get ("k2"), merge.Get ("k2"));
            Assert.NotEqual (opt3.Get ("k3"), merge.Get ("k3"));
            Assert.Equal (opt1.Get ("k2"), merge.Get ("k2"));
            Assert.Equal (opt1.Get ("k3"), merge.Get ("k3"));
            // alias
            Assert.Equal (opt1.Get ("k3"), merge.Get ("key3"));
            Assert.Equal (opt1.Get ("k3"), merge.Get ("3"));
        }
    }
}
