using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal static class TypeExtensionHelper
    {
        public static void MergeObjectFields(
            ITypeCompletionContext context,
            Type sourceType,
            IList<ObjectFieldDefinition> extensionFields,
            IList<ObjectFieldDefinition> typeFields)
        {
            MergeOutputFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (extensionField.Resolver != null)
                    {
                        typeField.Resolver = extensionField.Resolver;
                    }

                    if (extensionField.MiddlewareComponents.Count > 0)
                    {
                        foreach (FieldMiddleware component in
                            extensionField.MiddlewareComponents)
                        {
                            typeField.MiddlewareComponents.Add(component);
                        }
                    }
                },
                extensionField => extensionField.SourceType = sourceType);
        }

        public static void MergeInterfaceFields(
            ITypeCompletionContext context,
            IList<InterfaceFieldDefinition> extensionFields,
            IList<InterfaceFieldDefinition> typeFields)
        {
            MergeOutputFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) => { });
        }

        public static void MergeInputObjectFields(
            ITypeCompletionContext context,
            IList<InputFieldDefinition> extensionFields,
            IList<InputFieldDefinition> typeFields)
        {
            MergeFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) => { });
        }

        private static void MergeOutputFields<T>(
            ITypeCompletionContext context,
            IList<T> extensionFields,
            IList<T> typeFields,
            Action<IList<T>, T, T> action,
            Action<T> onBeforeAdd = null)
            where T : OutputFieldDefinitionBase
        {
            MergeFields(context, extensionFields, typeFields,
                (fields, extensionField, typeField) =>
                {
                    if (extensionField.IsDeprecated)
                    {
                        typeField.DeprecationReason =
                            extensionField.DeprecationReason;
                    }

                    MergeFields(
                        context,
                        extensionField.Arguments,
                        typeField.Arguments,
                        (args, extensionArg, typeArg) => { });

                    action(fields, extensionField, typeField);
                },
                onBeforeAdd);
        }

        private static void MergeFields<T>(
            ITypeCompletionContext context,
            IList<T> extensionFields,
            IList<T> typeFields,
            Action<IList<T>, T, T> action,
            Action<T> onBeforeAdd = null)
            where T : FieldDefinitionBase
        {
            foreach (T extensionField in extensionFields)
            {
                T typeField = typeFields.FirstOrDefault(t =>
                    t.Name.Equals(extensionField.Name));

                if (typeField == null)
                {
                    onBeforeAdd?.Invoke(extensionField);
                    typeFields.Add(extensionField);
                }
                else
                {
                    MergeDirectives(
                        context,
                        extensionField.Directives,
                        typeField.Directives);

                    MergeContextData(extensionField, typeField);

                    action(typeFields, extensionField, typeField);
                }
            }
        }

        public static void MergeDirectives(
            ITypeCompletionContext context,
            IList<DirectiveDefinition> extension,
            IList<DirectiveDefinition> type)
        {
            var directives =
                new List<(DirectiveType type, DirectiveDefinition def)>();

            foreach (DirectiveDefinition directive in type)
            {
                DirectiveType directiveType =
                    context.GetDirectiveType(directive.Reference);
                directives.Add((directiveType, directive));
            }

            foreach (DirectiveDefinition directive in extension)
            {
                MergeDirective(context, directives, directive);
            }

            type.Clear();

            foreach (DirectiveDefinition directive in
                directives.Select(t => t.def))
            {
                type.Add(directive);
            }
        }

        private static void MergeDirective(
            ITypeCompletionContext context,
            IList<(DirectiveType type, DirectiveDefinition def)> directives,
            DirectiveDefinition directive)
        {
            DirectiveType directiveType =
                context.GetDirectiveType(directive.Reference);

            if (directiveType != null)
            {
                if (directiveType.IsRepeatable)
                {
                    directives.Add((directiveType, directive));
                }
                else
                {
                    var entry = directives.FirstOrDefault(t => t.type == directiveType);
                    if (entry == default)
                    {
                        directives.Add((directiveType, directive));
                    }
                    else
                    {
                        int index = directives.IndexOf(entry);
                        directives[index] = (directiveType, directive);
                    }
                }
            }
        }

        public static void MergeContextData(
            DefinitionBase extension,
            DefinitionBase type)
        {
            if (extension.ContextData.Count > 0)
            {
                foreach (KeyValuePair<string, object> entry in extension.ContextData)
                {
                    type.ContextData[entry.Key] = entry.Value;
                }
            }
        }

        public static void MergeInterfaces(
            ObjectTypeDefinition extension,
            ObjectTypeDefinition type)
        {
            if (extension.Interfaces.Count > 0)
            {
                foreach (var interfaceReference in extension.Interfaces)
                {
                    type.Interfaces.Add(interfaceReference);
                }
            }

            if (extension.FieldBindingType != typeof(object))
            {
                type.KnownClrTypes.Add(extension.FieldBindingType);
            }
        }

        public static void MergeTypes(
            ICollection<ITypeReference> extensionTypes,
            ICollection<ITypeReference> typeTypes)
        {
            var set = new HashSet<ITypeReference>(typeTypes);

            foreach (ITypeReference reference in extensionTypes)
            {
                if (set.Add(reference))
                {
                    typeTypes.Add(reference);
                }
            }
        }

        public static void MergeConfigurations(
            ICollection<ILazyTypeConfiguration> extensionConfigurations,
            ICollection<ILazyTypeConfiguration> typeConfigurations)
        {
            foreach (ILazyTypeConfiguration configuration in extensionConfigurations)
            {
                typeConfigurations.Add(configuration);
            }
        }
    }
}
