using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using EntityGraphQL.Compiler.Util;
using EntityGraphQL.Extensions;
using EntityGraphQL.Schema;
using HotChocolate.Language;

namespace EntityGraphQL.Compiler
{
    public static class QueryWalkerHelper
    {
        public static readonly Regex GuidRegex = new(@"^[0-9A-F]{8}[-]?([0-9A-F]{4}[-]?){3}[0-9A-F]{12}$", RegexOptions.IgnoreCase);

        public static object? ProcessArgumentValue(ISchemaProvider schema, IValueNode argumentValue, string argName, Type argType)
        {
            object? argValue = null;
            if (argumentValue.Value != null)
            {
                switch (argumentValue.Kind)
                {
                    case SyntaxKind.IntValue:
                        argValue = argType switch
                        {
                            _ when argType == typeof(short) || argType == typeof(short?) => short.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(ushort) || argType == typeof(ushort?) => ushort.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(int) || argType == typeof(int?) => int.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(uint) || argType == typeof(uint?) => uint.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(long) || argType == typeof(long?) => long.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(ulong) || argType == typeof(ulong?) => ulong.Parse(argumentValue.Value.ToString()!),
                            _ when argType == typeof(float) || argType == typeof(float?) => float.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ when argType == typeof(decimal) || argType == typeof(decimal?) => decimal.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ when argType == typeof(double) || argType == typeof(double?) => double.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ => argValue
                        };
                        break;
                    // these ones are the correct type
                    case SyntaxKind.StringValue:
                        argValue = (string)argumentValue.Value;
                        break;
                    case SyntaxKind.BooleanValue:
                        argValue = argumentValue.Value;
                        break;
                    case SyntaxKind.NullValue:
                        argValue = null;
                        break;
                    case SyntaxKind.EnumValue:
                        argValue = (string)argumentValue.Value;
                        break;
                    case SyntaxKind.ListValue:
                        argValue = ProcessListArgument(schema, (List<IValueNode>)argumentValue.Value, argName, argType);
                        break;
                    case SyntaxKind.ObjectValue:
                        {
                            // this should be an Input type
                            var obj = Activator.CreateInstance(argType);
                            argValue = ProcessObjectValue(schema, argumentValue, argName, argType, obj);
                        }
                        break;
                    case SyntaxKind.FloatValue:
                        argValue = argType switch
                        {
                            _ when argType == typeof(float) || argType == typeof(float?) => float.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ when argType == typeof(decimal) || argType == typeof(decimal?) => decimal.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ when argType == typeof(double) || argType == typeof(double?) => double.Parse(argumentValue.Value.ToString()!, CultureInfo.InvariantCulture),
                            _ => argValue
                        };
                        break;
                }
            }

            return ExpressionUtil.ChangeType(argValue, argType, schema);
        }

        private static object ProcessObjectValue(ISchemaProvider schema, IValueNode argumentValue, string argName, Type argType, object obj)
        {
            object argValue;
            var schemaType = schema.GetSchemaType(argType, null);
            foreach (var item in (List<ObjectFieldNode>)argumentValue.Value!)
            {
                if (!schemaType.HasField(item.Name.Value, null))
                    throw new EntityGraphQLCompilerException($"Field {item.Name.Value} not found of type {schemaType.Name}");
                var schemaField = (Field)schemaType.GetField(item.Name.Value, null);

                if (schemaField.ResolveExpression == null)
                    throw new EntityGraphQLCompilerException($"Field {item.Name.Value} on type {schemaType.Name} has no resolve expression");

                var nameFromType = ((MemberExpression)schemaField.ResolveExpression).Member.Name;
                var prop = argType.GetProperty(nameFromType);

                if (prop == null)
                {
                    var field = argType.GetField(nameFromType);
                    if (field == null)
                        throw new EntityGraphQLCompilerException($"Field {item.Name.Value} not found on object argument");
                    field.SetValue(obj, ProcessArgumentValue(schema, item.Value, argName, field.FieldType));
                }
                else
                {
                    prop.SetValue(obj, ProcessArgumentValue(schema, item.Value, argName, prop.PropertyType));
                }
            }
            argValue = obj;
            return argValue;
        }

        public static object ProcessListArgument(ISchemaProvider schema, List<IValueNode> values, string argName, Type fieldArgType)
        {
            var list = (IList)Activator.CreateInstance(fieldArgType);
            var listType = list.GetType().GetEnumerableOrArrayType();
            if (listType == null)
                throw new EntityGraphQLCompilerException($"Argument {argName} is not a list");

            foreach (var item in values)
            {
                list.Add(ProcessArgumentValue(schema, item, argName, listType));
            }

            return list;
        }
    }
}