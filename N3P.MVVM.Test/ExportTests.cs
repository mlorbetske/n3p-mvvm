using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace N3P.MVVM.Test
{
    [TestClass]
    public class ExportTests
    {
        public class MyEntity : BindableBase<MyEntity>
        {
            public string Foo
            {
                get { return Get(x => x.Foo); }
                set { Set(x => x.Foo, value); }
            }
            
            public MyOtherEntity Entity
            {
                get { return Get(x => x.Entity); }
                set { Set(x => x.Entity, value); }
            }
	    

            public class MyOtherEntity : BindableBase<MyOtherEntity>
            {
                public int A
                {
                    get { return Get(x => x.A); }
                    set { Set(x => x.A, value); }
                }
            }
        }

        public class MyEntity2 : BindableBase<MyEntity2>
        {
            [Initialize.Initialize]
            public Dictionary<string, List<int>> Mess
            {
                get { return Get(x => x.Mess); }
            }
        }

        [TestMethod]
        public void ValidateExport()
        {
            var e = new MyEntity
            {
                Foo = "Hi",
                Entity = new MyEntity.MyOtherEntity
                {
                    A = 5
                }
            };

            var eState = e.ExportState();
            e.Foo = "asodifj";
            e.Entity.A = 10;
            e = (MyEntity) eState.Apply();
            Assert.AreEqual("Hi", e.Foo);
            Assert.AreEqual(5, e.Entity.A);

            var e2 = new MyEntity2();
            e2.Mess["Hi"] = new List<int> {1, 2, 3};
            var estate21 = e2.ExportState();
            e2.Mess["There"] = new List<int> {4, 5, 6};
            var estate22 = e2.ExportState();
            e2.Mess["Hi"].RemoveAt(1);
            var estate23 = e2.ExportState();

            e2 = (MyEntity2) estate21.Apply();
            Assert.AreEqual(1, e2.Mess.Count);
            Assert.AreEqual(3, e2.Mess["Hi"].Count);
            Assert.IsTrue(new[] {1, 2, 3}.SequenceEqual(e2.Mess["Hi"]));

            e2 = (MyEntity2) estate23.Apply();
            Assert.AreEqual(2, e2.Mess.Count);
            Assert.AreEqual(2, e2.Mess["Hi"].Count);
            Assert.IsTrue(new[] { 1, 3 }.SequenceEqual(e2.Mess["Hi"]));

            e2 = (MyEntity2)estate22.Apply();
            Assert.AreEqual(2, e2.Mess.Count);
            Assert.AreEqual(3, e2.Mess["Hi"].Count);
            Assert.IsTrue(new[] { 1, 2, 3 }.SequenceEqual(e2.Mess["Hi"]));
        }
    }
}
