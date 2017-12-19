using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TypeLite.Tests.RegressionTests {
    public class Issue24_NullableEnumProperty {
        [Fact]
        public void WhenClassContainsNullableEnumProperty_ExceptionIsNotThrown() {
            var builder = new TsModelBuilder();
            builder.Add<Issue24Example>();

            var generator = new TsGenerator();
            var model = builder.Build();

            var ex = Record.Exception(() => generator.Generate(model));
            Assert.Null(ex);
        }
    }

    public class Issue24Example {
        public Issue24Enum? EnumProperty { get; set; }
    }

    public enum Issue24Enum {
        A = 1,
        B = 2
    }
}
