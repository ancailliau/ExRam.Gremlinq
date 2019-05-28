﻿using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using ExRam.Gremlinq.Core.Extensions;
using LanguageExt;

namespace ExRam.Gremlinq.Core
{
    public static class GraphElementPropertyModel
    {
        private sealed class GraphElementPropertyModelImpl : IGraphElementPropertyModel
        {
            public GraphElementPropertyModelImpl(IImmutableDictionary<MemberInfo, PropertyMetadata> metadata)
            {
                Metadata = metadata;
            }

            public IImmutableDictionary<MemberInfo, PropertyMetadata> Metadata { get; }
        }

        private sealed class DefaultGraphElementPropertyModel : IGraphElementPropertyModel
        {
            public IImmutableDictionary<MemberInfo, PropertyMetadata> Metadata => ImmutableDictionary<MemberInfo, PropertyMetadata>.Empty;
        }

        public static readonly IGraphElementPropertyModel Default = new DefaultGraphElementPropertyModel();

        public static IGraphElementPropertyModel ConfigureNames(this IGraphElementPropertyModel model, Func<MemberInfo, Option<string>, Option<string>> overrideTransformation)
        {
            return model.WithMetadata(_ => _.ConfigureNames(overrideTransformation));
        }
        
        public static IGraphElementPropertyModel WithCamelCaseNames(this IGraphElementPropertyModel model)
        {
            return model.ConfigureNames((member, name) => name.IfNone(member.Name).ToCamelCase());
        }

        public static IGraphElementPropertyModel WithLowerCaseNames(this IGraphElementPropertyModel model)
        {
            return model.ConfigureNames((member, name) => name.IfNone(member.Name).ToLower());
        }

        public static IGraphElementPropertyModel ConfigureElement<TElement>(this IGraphElementPropertyModel model, Func<IPropertyMetadataConfigurator<TElement>, IImmutableDictionary<MemberInfo, PropertyMetadata>> action)
            where TElement : class
        {
            return new GraphElementPropertyModelImpl(action(new PropertyMetadataConfigurator<TElement>(model.Metadata)));
        }

        internal static object GetIdentifier(this IGraphElementPropertyModel model, MemberInfo member)
        {
            var identifier = model.Metadata
                .TryGetValue(member)
                .Bind(x => x.NameOverride)
                .IfNone(member.Name);
            
            if (string.Equals(identifier, "id", StringComparison.OrdinalIgnoreCase))
                return T.Id;

            if (string.Equals(identifier, "label", StringComparison.OrdinalIgnoreCase))
                return T.Label;

            return identifier;
        }

        internal static IGraphElementPropertyModel FromGraphElementModels(params IGraphElementModel[] models)
        {
            return new GraphElementPropertyModelImpl(
                models
                    .SelectMany(model => model.Metadata.Keys)
                    .SelectMany(x => x.GetTypeHierarchy())
                    .Distinct()
                    .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                    .ToImmutableDictionary(
                        property => property,
                        property => string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase)
                            ? new PropertyMetadata(default, SerializationBehaviour.IgnoreOnUpdate)
                            : PropertyMetadata.Default,
                        MemberInfoEqualityComparer.Instance));
        }

        private static IGraphElementPropertyModel WithMetadata(this IGraphElementPropertyModel model, Func<IImmutableDictionary<MemberInfo, PropertyMetadata>, IImmutableDictionary<MemberInfo, PropertyMetadata>> transformation)
        {
            return new GraphElementPropertyModelImpl(
                transformation(model.Metadata));
        }
    }
}