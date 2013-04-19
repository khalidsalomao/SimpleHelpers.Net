using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PerformanceTest
{
    public class MockObjectGen
    {
        static Int64 uniqueCounter = 0;

        public static MockUser GetTestUserDefinition (string login, string group, bool admin, Int64 index)
        {
            var user = new MockUser
            {
                Login = login,
                Name = login,
                Desc = "TestUser",
                Password = "TestGroup",
                IsEnabled = true,
                IsSystemAdmin = admin,
                Group = group
            };
            var v = index.ToString ();
            user.Parameters.Add (v, v);
            user.List.Add (new KeyValueObj { Key = v, Value = v});
            return user;
        }

        public static IEnumerable<MockUser> GetTestUserDefinition (int count, string group, bool admin)
        {
            var prefix = "test_" + (uniqueCounter++).ToString () + "_";
            for (var i = 0; i < count; i++)
            {
                yield return GetTestUserDefinition (prefix + i, group, admin, uniqueCounter);
            }
        }

        public class MockUser
        {
            private Dictionary<string, string> m_parameters = new Dictionary<string, string> (StringComparer.Ordinal);

            private List<KeyValueObj> m_list = new List<KeyValueObj> ();

            public object Id { get; set; }

            public string Login { get; set; }

            public string Name { get; set; }
            public string SessionId { get; set; }
            public string Group { get; set; }
            public string Desc { get; set; }
            public string Tel1 { get; set; }
            public string Cel1 { get; set; }
            public string Email { get; set; }
            public bool IsEnabled { get; set; }
            public string Password { get; set; }
            public string Question { get; set; }
            public string QuestionAnswer { get; set; }
            public bool IsSystemAdmin { get; set; }
            public Dictionary<string, string> Parameters
            {
                get { return m_parameters; }
                set { m_parameters = value; }
            }

            public List<KeyValueObj> List
            {
                get { return m_list; }
                set { m_list = value; }
            }
        }

        public class KeyValueObj
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
