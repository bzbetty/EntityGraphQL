---
sidebar_position: 1
---

# Schema Directives

Schema Directives are used to decorate the schema to provide more information to clients, EntityGraphQL can also use this information to update the introspection model or provide extra functionality such as validation.

## Built-In Directives

The GraphQL spec defines directives that are supported out of the box in EntityGraphQL.

- `@deprecated(reason: String)` - Tells the client that this type, field or enum value should no longer be used along with the reason why or a suggested alternative
- `@oneOf` - Mark the input type as a type where only one of it's fields should ever be non-null
- `@specifiedBy(url: String)` - Used to provide a scalar specification URL for specifying the behavior of custom scalar types.

## Deprecated

Fields, Enum Values, Mutation Arguments and Types marked with the c# [ObsoleteAttribute] will have the [@deprecated](https://spec.graphql.org/draft/#sec--deprecated) directive automatically added.

You can also use the Deprecate(string reason) extension method on a IField.

## One Of Input Types

EntityGraphQL supports [One Of Input Types](https://github.com/graphql/graphql-spec/pull/825).

Mark an input type with `GraphQLOneOfAttribute` and EntityGraphQL will mark the type with `@oneOf` in the schema and validate the input meets the requiements on execution.

```cs
[GraphQLOneOf]
private class OneOfInputType
{
    public int? One { get; set; }
    public int? Two { get; set; }
}
```

This will generate the follow graphql schema.

```graphql
input MutationArgs @oneOf {
  one: Int
  two: Int
}
```

Although each field on the input is nullable the `@oneOf` input type has one further validation step.

- Exactly one key must be specified

## Specified By

The [@specifiedBy](https://spec.graphql.org/draft/#sec--specifiedBy) directive is used to provide extra information about a custom scalar type. The URL should point to a human-readable specification of the data format, serialization, and coercion rules. It must not appear on built-in scalar types.

## Custom Directives

Like [Field Extensions](../field-extensions/) you can extend the [ExtensionAttribute](../other-extensibility/extension-attribute) to apply extension to `IFields` and `ISchemaTypes` (see `GraphQLOneOfAttribute`).

IFields and ISchema types also have a method `AddDirective` that is common to call in the attribute extension method or a custom extension method to register the directive.

```
public static class DeprecatedDirectiveExtensions
{
    public static void Deprecate(this IField field, string? reason)
    {
        field.AddDirective(new DeprecatedDirective(reason));
    }
}
public class ObsoleteAttributeRegistration : IExtensionAttribute<ObsoleteAttribute>
{
    public void ApplyExtension(IField field, ObsoleteAttribute attribute)
    {
        field.Deprecate(attribute.Message);
    }
}
```

The `ISchemaDirective` method contains:

* The `On` property for marking where it is valid to use this directive
* The `ToGraphQLSchemaString()` method where you return the formatted directive including arguments (eg `return $"@deprecated(reason: \"{Reason}\")";`)
* A series of methods the SchemaInstrospection class calls allowing you to modify the introspection result (primarily used because instrospection for @deprecated and @oneOf is done using specific fields and not yet in an extensible way)


## Example Sample Directive

Finds the ASP.NET [Authorize] attribute and adds @authorize(roles: String, policies: String) to the schema information

```
public class AuthorizeAttributeRegistration : 
    IExtensionAttribute<AuthorizeAttribute>,
    IExtensionAttribute<GraphQLAuthorizePolicyAttribute>
{
    public void ApplyExtension(IField field, AuthorizeAttribute attribute)
    {
        field.AddDirective(new AuthorizeDirective(attribute));
    }
    public void ApplyExtension(IField field, GraphQLAuthorizePolicyAttribute attribute)
    {
        field.AddDirective(new AuthorizeDirective(attribute));
    }
    public void ApplyExtension(ISchemaType type, AuthorizeAttribute attribute)
    {
        type.AddDirective(new AuthorizeDirective(attribute));
    }
    public void ApplyExtension(ISchemaType type, GraphQLAuthorizePolicyAttribute attribute)
    {
        type.AddDirective(new AuthorizeDirective(attribute));
    }
}
public class AuthorizeDirective : ISchemaDirective
{
    public AuthorizeDirective(AuthorizeAttribute authorize)
    {
        Roles = authorize.Roles;
        Policies = new List<string>() { authorize.Policy! };
    }
    public AuthorizeDirective(GraphQLAuthorizePolicyAttribute authorize)
    {
        Policies = authorize.Policies;
    }
    public IEnumerable<TypeSystemDirectiveLocation> On => new[] {                
        TypeSystemDirectiveLocation.OBJECT,
        TypeSystemDirectiveLocation.FIELD_DEFINITION            
    };
    public string? Roles { get; }
    public List<string> Policies { get; }
    public string ToGraphQLSchemaString()
    {
        return $"@authorize(roles: \"{Roles}\", policies: \"{string.Join(", ", Policies)}\")";
    }
}
```