using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Csi3
{
    class ScriptReferenceResolver : MetadataReferenceResolver, IEquatable<ScriptReferenceResolver>
    {
        public static ScriptReferenceResolver Default = new ScriptReferenceResolver();

        public ScriptReferenceResolver WithSearchPaths(params string[] paths)
        {
            var resolver = new ScriptReferenceResolver();
            resolver._searchPaths = _searchPaths;
            return resolver;
        }

        public ScriptReferenceResolver WithSearchPaths(IEnumerable<string> paths)
        {
            var resolver = new ScriptReferenceResolver();
            resolver._searchPaths = _searchPaths;
            return resolver;
        }

        public bool Equals(ScriptReferenceResolver other)
        {
            return ReferenceEquals(this, other) ||
                other != null &&
                Equals(_defaultResolver, other._defaultResolver);
        }

        public override bool ResolveMissingAssemblies
            => _defaultResolver.ResolveMissingAssemblies;

        public override bool Equals(object other)
            => Equals(other as ScriptReferenceResolver);

        public override int GetHashCode()
            => _defaultResolver.GetHashCode();

        public override PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity)
            => _defaultResolver.ResolveMissingAssembly(definition, referenceIdentity);

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties)
        {
            var resolvedReferences = _defaultResolver.ResolveReference(reference, baseFilePath, properties);
            if (resolvedReferences.Length > 0)
            {
                return resolvedReferences;
            }

            foreach (var searchPath in _searchPaths)
            {
                resolvedReferences = resolvedReferences.AddRange(
                    _defaultResolver.ResolveReference(
                        Path.Combine(searchPath, reference),
                        baseFilePath,
                        properties));
            }
            return resolvedReferences;
        }

        private MetadataReferenceResolver _defaultResolver = ScriptOptions.Default.MetadataResolver;
        private ImmutableArray<string> _searchPaths = ImmutableArray<string>.Empty;
    }
}
