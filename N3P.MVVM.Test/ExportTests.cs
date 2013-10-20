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
            eState.Apply(e);
            Assert.AreEqual("Hi", e.Foo);
            Assert.AreEqual(5, e.Entity.A);
        }
    }
}
