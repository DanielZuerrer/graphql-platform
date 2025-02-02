namespace HotChocolate.Types;

/// <summary>
/// The cost directive can be used to express the expected
/// cost that a resolver incurs on the system.
/// </summary>
public sealed class CostDirectiveType : DirectiveType<CostDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<CostDirective> descriptor)
    {
        descriptor
            .Name("cost")
            .Description(
                "The cost directives is used to express the complexity " +
                "of a field.")
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(t => t.Complexity)
            .Name("complexity")
            .Description("Defines the complexity of the field.")
            .Type<NonNullType<IntType>>()
            .DefaultValue(1);

        descriptor
            .Argument(t => t.Multipliers)
            .Name("multipliers")
            .Description(
                "Defines field arguments that act as " +
                 "complexity multipliers.")
            .Type<ListType<NonNullType<MultiplierPathType>>>();

        descriptor
            .Argument(t => t.DefaultMultiplier)
            .Name("defaultMultiplier")
            .Description("Gets the default multiplier.")
            .Type<IntType>();
    }
}
