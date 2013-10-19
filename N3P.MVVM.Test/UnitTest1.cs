using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using N3P.MVVM.Dirty;

namespace N3P.MVVM.Test
{
    [TestClass]
    public class DirtyableTests
    {
        [Dirtyable]
        public class Top : BindableBase<Top>
        {
            public Uri Value
            {
                get { return Get(x => x.Value); }
                set { Set(x => x.Value, value); }
            }

            public Middle Mid
            {
                get { return Get(x => x.Mid); }
                set { Set(x => x.Mid, value); }
            }

            [Dirtyable]
            public class Middle : BindableBase<Middle>
            {
                public Uri Value
                {
                    get { return Get(x => x.Value); }
                    set { Set(x => x.Value, value); }
                }
            }
        }

        [TestMethod]
        [TestCategory("Dirtyable")]
        public void Generic()
        {
            var mid = new Top.Middle();
            Assert.IsTrue(mid.GetIsDirtyTracked());
            Assert.IsFalse(mid.GetIsDirty());
            mid.Value = new Uri("http://google.com");
            Assert.IsTrue(mid.GetIsDirty());
            mid.Clean();
            Assert.IsFalse(mid.GetIsDirty());

            var top = new Top
            {
                Value = new Uri("http://bing.com"),
                Mid = mid
            };
            Assert.IsTrue(top.GetIsDirty());
            top.Clean();
            Assert.IsFalse(top.GetIsDirty());
            Assert.IsFalse(mid.GetIsDirty());
            top.Mid.Value = new Uri("http://amazon.com");
            Assert.IsTrue(top.GetIsDirty());
        }
    }
}
