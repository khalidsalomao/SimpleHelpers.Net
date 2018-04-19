using SimpleHelpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace SimpleHelpersTests
{
	public class ObjectDiffPatchTest
	{
        public ObjectDiffPatchTest ()
        {
            // make sure we are using default settings
            ObjectDiffPatch.DefaultSerializerSettings = null;
        }

        [Fact]
        public void AbleToDiffAndPatchSimpleObject ()
		{
            var testObj = TestClass.CreateSimpleInstance ();

            var updatedTestObj = TestClass.CreateSimpleInstance ();
			updatedTestObj.StringProperty = "this is an updated string";
			updatedTestObj.IntProperty = 5678;
			updatedTestObj.DoubleProperty = 123.456;

            var diff = ObjectDiffPatch.GenerateDiff (testObj, updatedTestObj);

			var revertPatch = diff.OldValues.ToString ();

			var revertedObj = ObjectDiffPatch.PatchObject(updatedTestObj, revertPatch);

			Assert.Equal (testObj.StringProperty, revertedObj.StringProperty);
			Assert.Equal (testObj.IntProperty, revertedObj.IntProperty);
			Assert.Equal (testObj.DoubleProperty, revertedObj.DoubleProperty);
		}

        [Fact]
        public void AbleToDeleteStringListItemThenRevertViaPatch ()
		{
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.PopulateStringListOnTestClass ();

            var updatedTestObj = TestClass.CreateSimpleInstance ();
            updatedTestObj.PopulateStringListOnTestClass ();

			updatedTestObj.ListOfStringProperty.Remove("list");

			Assert.NotEqual(testObj.ListOfStringProperty, updatedTestObj.ListOfStringProperty);


			var diff = ObjectDiffPatch.GenerateDiff(testObj, updatedTestObj);

			var revertPatch = diff.OldValues.ToString ();

			var revertedObj = ObjectDiffPatch.PatchObject(updatedTestObj, revertPatch);

			Assert.Equal (testObj.ListOfStringProperty, revertedObj.ListOfStringProperty);
		}

        [Fact]
        public void AbleToDeleteObjectListItemThenRevertViaPatch ()
		{
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.PopulateObjectListOnTestClass ();

            var updatedTestObj = TestClass.CreateSimpleInstance ();
            updatedTestObj.PopulateObjectListOnTestClass ();

			updatedTestObj.ListOfObjectProperty.RemoveAt(1);

			Assert.NotEqual(testObj.ListOfObjectProperty.Count, updatedTestObj.ListOfObjectProperty.Count);

			var diff = ObjectDiffPatch.GenerateDiff(testObj, updatedTestObj);

			var revertPatch = diff.OldValues.ToString ();

			var revertedObj = ObjectDiffPatch.PatchObject(updatedTestObj, revertPatch);

			Assert.Equal (testObj.ListOfObjectProperty.Count, revertedObj.ListOfObjectProperty.Count);
		}

        [Fact]
        public void AbleToEditObjectInListThenRevertViaPatch ()
		{
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.PopulateObjectListOnTestClass ();

            var updatedTestObj = TestClass.CreateSimpleInstance ();
            updatedTestObj.PopulateObjectListOnTestClass ();

			updatedTestObj.ListOfObjectProperty[2].IntProperty = 30;
			updatedTestObj.ListOfObjectProperty[2].StringProperty = "this is an update to the last object in the list";
			updatedTestObj.ListOfObjectProperty[2].DoubleProperty = 33.333;

			var diff = ObjectDiffPatch.GenerateDiff(testObj, updatedTestObj);

			var revertPatch = diff.OldValues.ToString ();

			var revertedObj = ObjectDiffPatch.PatchObject(updatedTestObj, revertPatch);

			Assert.Equal(testObj.ListOfObjectProperty[2].IntProperty, revertedObj.ListOfObjectProperty[2].IntProperty);
			Assert.Equal(testObj.ListOfObjectProperty[2].StringProperty, revertedObj.ListOfObjectProperty[2].StringProperty);
			Assert.Equal(testObj.ListOfObjectProperty[2].DoubleProperty, revertedObj.ListOfObjectProperty[2].DoubleProperty);
		}

        [Fact]
        public void AbleToAddObjectListItemThenApplyViaPatch ()
        {
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.PopulateObjectListOnTestClass ();

            var updatedTestObj = TestClass.CreateSimpleInstance ();
            updatedTestObj.PopulateObjectListOnTestClass ();

            updatedTestObj.ListOfObjectProperty.Add(new TestClass { StringProperty = "added" });

            var diff = ObjectDiffPatch.GenerateDiff(testObj, updatedTestObj);

            var updatePatch = diff.NewValues.ToString ();

            var objToUpdate = TestClass.CreateSimpleInstance ();
            objToUpdate.PopulateObjectListOnTestClass ();

            var updatedObj = ObjectDiffPatch.PatchObject(objToUpdate, updatePatch);

            Assert.Equal(updatedTestObj.ListOfObjectProperty.Count, updatedObj.ListOfObjectProperty.Count);

            var addedListItem = updatedObj.ListOfObjectProperty.SingleOrDefault(obj => obj != null && obj.StringProperty == "added");

            Assert.NotNull(addedListItem);

        }

        [Fact]
        public void AbleToSnapshotSimpleObject ()
        {
            var testObj = TestClass.CreateSimpleInstance ();
            var snapshot = ObjectDiffPatch.Snapshot (testObj);

            // update the original instance
            testObj.StringProperty = "this is an updated string";

            var diff = ObjectDiffPatch.GenerateDiff (snapshot, testObj);            
            Assert.Equal (diff.NewValues.Value<string> ("StringProperty"), testObj.StringProperty);
            Assert.Equal (diff.OldValues.Value<string> ("StringProperty"), snapshot.Value<string> ("StringProperty"));

            diff = ObjectDiffPatch.GenerateDiff (snapshot, ObjectDiffPatch.Snapshot (testObj));
            Assert.Equal (diff.NewValues.Value<string> ("StringProperty"), testObj.StringProperty);
            Assert.Equal (diff.OldValues.Value<string> ("StringProperty"), snapshot.Value<string> ("StringProperty"));
        }

        [Fact]
        public void AbleToCompareWithNull ()
        {
            var testObj = TestClass.CreateSimpleInstance ();
            
            var diff = ObjectDiffPatch.GenerateDiff (testObj, null);
            Assert.Equal (diff.OldValues.Value<string> ("StringProperty"), testObj.StringProperty);
            Assert.Null (diff.NewValues);

            diff = ObjectDiffPatch.GenerateDiff (null, testObj);
            Assert.Equal (diff.NewValues.Value<string> ("StringProperty"), testObj.StringProperty);
            Assert.Null (diff.OldValues);
        }

        [Fact]
        public void AbleToDiffAndPathDictionary ()
        {
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.Map = new Dictionary<string, string> ();
            testObj.Map.Add ("1", "one");
            testObj.Map.Add ("2", "two");
            testObj.Map.Add ("3", "three");
            testObj.Map.Add ("4", "four");

            testObj.Childs = new Dictionary<string, TestClass> ();
            testObj.Childs.Add ("c1", new TestClass { StringProperty = "one", IntProperty = 1 });
            testObj.Childs.Add ("c2", new TestClass { StringProperty = "two", IntProperty = 2 });
            testObj.Childs.Add ("c3", new TestClass { StringProperty = "three", IntProperty = 3 });

            var snapshot = ObjectDiffPatch.Snapshot (testObj);

            testObj.Map["2"] = "updated string";
            testObj.Map.Remove ("3");
            testObj.Map.Remove ("4");
            testObj.Map.Add ("5", "five");
            testObj.Map.Add ("6", "six");

            testObj.Childs["c1"].IntProperty = 2;
            testObj.Childs["c2"].StringProperty = "updated string";
            testObj.Childs["c2"].IntProperty = 100;
            testObj.Map.Remove ("c3");
            testObj.Childs.Add ("c4", TestClass.CreateSimpleInstance ());
            testObj.Childs.Add ("c5", TestClass.CreateSimpleInstance ());

            var diff = ObjectDiffPatch.GenerateDiff (snapshot, testObj);

            var revertedObj = ObjectDiffPatch.PatchObject (testObj, diff.OldValues.ToString ());

            Assert.Equal ("one", revertedObj.Map["1"]);
            Assert.Equal ("two", revertedObj.Map["2"]);
            Assert.Equal ("three", revertedObj.Map["3"]);
            Assert.Equal ("four", revertedObj.Map["4"]);
            Assert.False (revertedObj.Map.ContainsKey ("5"));
            Assert.False (revertedObj.Map.ContainsKey ("6"));

            Assert.Equal ("one", revertedObj.Childs["c1"].StringProperty);
            Assert.Equal (1, revertedObj.Childs["c1"].IntProperty);
            Assert.Equal ("two", revertedObj.Childs["c2"].StringProperty);
            Assert.Equal (2, revertedObj.Childs["c2"].IntProperty);            
            Assert.Equal ("three", revertedObj.Childs["c3"].StringProperty);
            Assert.False (revertedObj.Map.ContainsKey ("c4"));
            Assert.False (revertedObj.Map.ContainsKey ("c5"));
        }

        [Fact]
        public void AbleToDiffAndPathListOfObjects ()
        {
            var testObj = TestClass.CreateSimpleInstance ();
            testObj.ListOfObjectProperty = new List<TestClass> ();
            testObj.ListOfObjectProperty.Add (new TestClass { StringProperty = "one", IntProperty = 1 });
            testObj.ListOfObjectProperty.Add (new TestClass { StringProperty = "two", IntProperty = 2, Map = new Dictionary<string, string> { { "m1", "1" }, { "m2", "2" } } });
            testObj.ListOfObjectProperty.Add (new TestClass { StringProperty = "three", IntProperty = 3, Map = new Dictionary<string,string> { {"m1", "1"} }});
            testObj.ListOfObjectProperty.Add (new TestClass { StringProperty = "four", IntProperty = 4 });

            var snapshot = ObjectDiffPatch.Snapshot (testObj);

            testObj.ListOfObjectProperty.Add (testObj.ListOfObjectProperty[0]);
            testObj.ListOfObjectProperty.RemoveAt (0);

            var diff = ObjectDiffPatch.GenerateDiff (snapshot, testObj);

            var revertedObj = ObjectDiffPatch.PatchObject (testObj, diff.OldValues.ToString ());

            Assert.Equal ("one", revertedObj.ListOfObjectProperty[0].StringProperty);
            Assert.Equal ("two", revertedObj.ListOfObjectProperty[1].StringProperty);
        }

        [Fact]
        public void AbleToHandleCircularReferences_Ignore ()
        {
            CircularObject original = new CircularObject ();
            original.AddChild ();
            original.AddChild ();
            original.FirstChild.AddChild ();

            var snapshot = ObjectDiffPatch.Snapshot (original).ToString ();

            ObjectDiffPatch.DefaultSerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All;

            var snapshotWithRefs = ObjectDiffPatch.Snapshot (original).ToString ();

            Assert.NotEqual (snapshot, snapshotWithRefs);

            var updated = ObjectDiffPatch.PatchObject (new CircularObject (), snapshotWithRefs);
            // they should be different, since all circular references are ignored!
            Assert.NotEqual (updated, original);
        }

        [Fact]
        public void AbleToHandleCircularReferences_All ()
        {
            CircularObject original = new CircularObject ();
            original.AddChild ();
            original.AddChild ();
            original.FirstChild.AddChild ();

            var snapshot = ObjectDiffPatch.Snapshot (original).ToString ();

            ObjectDiffPatch.DefaultSerializerSettings.PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.All;
            // see http://www.newtonsoft.com/json/help/html/PreserveReferencesHandlingObject.htm

            var snapshotWithRefs = ObjectDiffPatch.Snapshot (original).ToString ();

            Assert.NotEqual (snapshot, snapshotWithRefs);

            var updated = ObjectDiffPatch.PatchObject (new CircularObject (), snapshotWithRefs);
           // they should be equal
            Assert.Equal (Newtonsoft.Json.JsonConvert.SerializeObject (updated, ObjectDiffPatch.DefaultSerializerSettings),
                Newtonsoft.Json.JsonConvert.SerializeObject (original, ObjectDiffPatch.DefaultSerializerSettings));
            Assert.NotNull (updated.FirstChild);
            Assert.NotNull (updated.FirstChild.Parent);
            Assert.NotNull (updated.FirstChild.FirstChild);

            // revertBack
            var diff = ObjectDiffPatch.GenerateDiff (original, new CircularObject ());

            updated = ObjectDiffPatch.PatchObject (original, diff.NewValues);
            //ensure everything is alright
            Assert.Null (updated.FirstChild);
            Assert.Null (updated.Parent);
            Assert.Null (updated.Children);
        }

        [Fact]
        public void AbleToDiffEqualObjects()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.True(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffStringProperty()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.StringProperty = "123";
            target.StringProperty = "234";
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffSIntProperty()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.IntProperty = 123;
            target.IntProperty = 234;
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffDoubleProperty()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.DoubleProperty = 123;
            target.DoubleProperty = 234;
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfStrings()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfStringProperty = new List<string> { "1", "2" };
            target.ListOfStringProperty = new List<string> { "1", "3" };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfInt()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfIntProperty = new List<int> { 1, 2, 3 };
            target.ListOfIntProperty = new List<int> { 1, 2, 4 };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfDouble()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfDoubleProperty = new List<double> { 1, 2, 3 };
            target.ListOfDoubleProperty = new List<double> { 1, 2, 4 };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffStringPropertyWithSourceNull()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.StringProperty = "123";
            target.StringProperty = null;
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffStringPropertyWithTargetNull()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.StringProperty = null;
            target.StringProperty = "123";
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfStringsWithTargetNull()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfStringProperty = null;
            target.ListOfStringProperty = new List<string> { "1", "2" };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfStringsWithSourceNull()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfStringProperty = new List<string> { "1", "2" };
            target.ListOfStringProperty = null;
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfStringsWithNullItemInSource()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfStringProperty = new List<string> { "1", null };
            target.ListOfStringProperty = new List<string> { "1", "0" };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

        [Fact]
        public void AbleToDiffListOfStringsWithNullItemInTarget()
        {
            var source = TestClass.CreateSimpleInstance();
            var target = TestClass.CreateSimpleInstance();
            source.ListOfStringProperty = new List<string> { "1", "2" };
            target.ListOfStringProperty = new List<string> { "1", null };
            var diff = ObjectDiffPatch.GenerateDiff(source, target);
            Assert.False(diff.AreEqual);
        }

    }




    class CircularObject
    {
        public CircularObject FirstChild { get; set; }
        public List<CircularObject> Children { get; set; }
        public CircularObject Parent { get; set; }
        public CircularObject AddChild ()
        {
            var item = new CircularObject () { Parent = this };
            if (FirstChild == null)
                FirstChild = item;
            if (Children == null)
                Children = new List<CircularObject> ();
            Children.Add (item);
            return item;
        }
        //public CircularObject (CircularObject parent)
        //{
        //    Parent = parent;
        //}
    }

	class TestClass
	{
		public string StringProperty { get; set; }
		public int IntProperty { get; set; }
		public double DoubleProperty { get; set; }
		public List<TestClass> ListOfObjectProperty { get; set; }
		public List<string> ListOfStringProperty { get; set; }
		public List<int> ListOfIntProperty { get; set; }
		public List<double> ListOfDoubleProperty { get; set; }
        public Dictionary<string, string> Map { get; set; }
        public Dictionary<string, TestClass> Childs { get; set; }

        public static TestClass CreateSimpleInstance ()
        {
            return new TestClass ()
            {
                StringProperty = "this is a string",
                IntProperty = 1234,
                DoubleProperty = 56.789
            };
        }

        public void PopulateStringListOnTestClass ()
        {
            ListOfStringProperty = new List<string> ()
			{
				"this", "is", "a", "list", "of", "strings"
			};
        }

        public void PopulateObjectListOnTestClass ()
        {
            ListOfObjectProperty = new List<TestClass> ()
			{
			    new TestClass()
			    {
				    StringProperty = "this is the first object",
				    IntProperty = 1,
				    DoubleProperty = 1.01
			    },
			    new TestClass()
			    {
				    StringProperty = "this is the second object",
				    IntProperty = 2,
				    DoubleProperty = 2.02
			    },
			    new TestClass()
			    {
				    StringProperty = "this is the third object",
				    IntProperty = 3,
				    DoubleProperty = 3.03
			    }
			};
        }
	}
}
