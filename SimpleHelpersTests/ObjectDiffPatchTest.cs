using SimpleHelpers;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Extensions;

namespace SimpleHelpersTests
{
	public class ObjectDiffPatchTest
	{
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
    }

	public class TestClass
	{
		public string StringProperty { get; set; }
		public int IntProperty { get; set; }
		public double DoubleProperty { get; set; }
		public List<TestClass> ListOfObjectProperty { get; set; }
		public List<string> ListOfStringProperty { get; set; }
		public List<int> ListOfIntProperty { get; set; }
		public List<double> ListOfDoubleProperty { get; set; }

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
