using Xunit;
using EntityGraphQL.Extensions;
using EntityGraphQL.Schema;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EntityGraphQL.Tests.Util
{
    public class NullableReferenceTypeTests
    {
        public class Test { }

        public class WithoutNullableRefEnabled
        {
            public int NonNullableInt { get; set; }
            public int? NullableInt { get; set; }
            public string Nullable { get; set; }
            public IEnumerable<Test> Tests { get; set; }
            public IEnumerable<Test> NullableMethod() { return null!; }
        }

        [Fact]
        public void TestNullableWithoutNullableRefEnabled()
        {
            var propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("NonNullableInt");
            Assert.False(propertyInfo.IsNullable());

            propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("NullableInt");
            Assert.True(propertyInfo.IsNullable());

            propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("Nullable");
            Assert.True(propertyInfo.IsNullable());

            propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("Tests");
            Assert.True(propertyInfo.IsNullable());

            var methodInfo = typeof(WithoutNullableRefEnabled).GetMethod("NullableMethod");
            Assert.True(methodInfo.IsNullable());

            var schema = SchemaBuilder.FromObject<WithoutNullableRefEnabled>();
            var schemaString = schema.ToGraphQLSchemaString();

            Assert.Contains(@"nonNullableInt: Int!", schemaString);
            Assert.Contains(@"nullableInt: Int", schemaString);
            Assert.Contains(@"nullable: String", schemaString);
            Assert.Contains(@"tests: [Test!]", schemaString);
        }

#nullable enable
        public class WithNullableRefEnabled
        {
            public int NonNullableInt { get; set; }
            public int? NullableInt { get; set; }
            public string NonNullable { get; set; } = "";
            public string? Nullable { get; set; }
            public IEnumerable<Test> Tests { get; set; } = new List<Test>();
            public IEnumerable<Test>? Tests2 { get; set; }
            public IEnumerable<Test> NonNullableMethod() { return null!; }
            public IEnumerable<Test?> NullableMethod() { return null!; }
        }
#nullable restore

        [Fact]
        public void TestNullableWithNullableRefEnabled()
        {
            var propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("NonNullableInt");
            Assert.False(propertyInfo.IsNullable());

            propertyInfo = typeof(WithoutNullableRefEnabled).GetProperty("NullableInt");
            Assert.True(propertyInfo.IsNullable());

            propertyInfo = typeof(WithNullableRefEnabled).GetProperty("Nullable");
            Assert.True(propertyInfo.IsNullable());

            propertyInfo = typeof(WithNullableRefEnabled).GetProperty("NonNullable");
            Assert.False(propertyInfo.IsNullable());

            var methodInfo = typeof(WithNullableRefEnabled).GetMethod("NullableMethod");
            Assert.True(methodInfo.IsNullable());

            methodInfo = typeof(WithNullableRefEnabled).GetMethod("NonNullableMethod");
            Assert.False(methodInfo.IsNullable());

            var schema = SchemaBuilder.FromObject<WithNullableRefEnabled>();
            var schemaString = schema.ToGraphQLSchemaString();

            Assert.Contains(@"nonNullableInt: Int!", schemaString);
            Assert.Contains(@"nullableInt: Int", schemaString);
            Assert.Contains(@"nullable: String", schemaString);
            Assert.Contains(@"nonNullable: String!", schemaString);
            Assert.Contains(@"tests: [Test!]!", schemaString);
            Assert.Contains(@"tests2: [Test!]", schemaString);


            var gql = new QueryRequest
            {
                Query = @"
                  query {
                    __type(name: ""Query"") {                        
                        fields {
                            name
                            type  { 
                                name
                                kind
                                ofType {
                                    name
                                    kind
                                }
                            }
                            args {
                                name 
                                type { name kind }
                            }
                        }
                    }
                  }
                "
            };

            var res = schema.ExecuteRequest(gql, new WithNullableRefEnabled(), null, null);
            Assert.Null(res.Errors);

            var type = (dynamic)res.Data["__type"];

            Assert.Equal(@"{""name"":""nullable"",""type"":{""name"":""String"",""kind"":""SCALAR"",""ofType"":null},""args"":[]}", JsonConvert.SerializeObject(type.fields[0]));
            Assert.Equal(@"{""name"":""nonNullable"",""type"":{""name"":null,""kind"":""NON_NULL"",""ofType"":{""name"":""String"",""kind"":""SCALAR""}},""args"":[]}", JsonConvert.SerializeObject(type.fields[1]));
            Assert.Equal(@"{""name"":""nonNullableInt"",""type"":{""name"":null,""kind"":""NON_NULL"",""ofType"":{""name"":""Int"",""kind"":""SCALAR""}},""args"":[]}", JsonConvert.SerializeObject(type.fields[2]));
            Assert.Equal(@"{""name"":""nullableInt"",""type"":{""name"":""Int"",""kind"":""SCALAR"",""ofType"":null},""args"":[]}", JsonConvert.SerializeObject(type.fields[3]));
            Assert.Equal(@"{""name"":""tests"",""type"":{""name"":null,""kind"":""NON_NULL"",""ofType"":{""name"":null,""kind"":""LIST""}},""args"":[]}", JsonConvert.SerializeObject(type.fields[4]));
            Assert.Equal(@"{""name"":""tests2"",""type"":{""name"":null,""kind"":""NON_NULL"",""ofType"":{""name"":null,""kind"":""LIST""}},""args"":[]}", JsonConvert.SerializeObject(type.fields[5]));
        }

    }
}