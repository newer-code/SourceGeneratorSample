namespace SourceGeneratorSample.IncrementalGenerator.Starters;

[Generator(LanguageNames.CSharp)]
public sealed class GreetingGenerator_UseAttribute : IIncrementalGenerator
{
	/// <inheritdoc/>
	public void Initialize(IncrementalGeneratorInitializationContext context)
		=> context.RegisterSourceOutput(
			context.SyntaxProvider
				.ForAttributeWithMetadataName(
					"SourceGeneratorSample.Greetings.SayHello2Attribute",
					static (node, _) => node is MethodDeclarationSyntax
					{
						Modifiers: var modifiers and not [],
						Parent: TypeDeclarationSyntax
						{
							Modifiers: var typeModifiers and not []
						}
					}
					&& modifiers.Any(SyntaxKind.PartialKeyword)
					&& typeModifiers.Any(SyntaxKind.PartialKeyword),
					(gasc, _) => gasc switch
					{
						{
							TargetNode: MethodDeclarationSyntax node,
							TargetSymbol: IMethodSymbol
							{
								Name: var methodName,
								TypeParameters: [],
								Parameters:
								[
									{
										Type.SpecialType: SpecialType.System_String,
										Name: var paramName
									}
								],
								ReturnsVoid: true,
								IsStatic: true,
								ContainingType:
								{
									Name: var typeName,
									ContainingNamespace: var @namespace,
									TypeKind: var typeKind and (TypeKind.Class or TypeKind.Struct or TypeKind.Interface)
								}
							}
						} => new GatheredData(methodName, paramName, typeName, @namespace, typeKind, node),
						_ => null
					}
				)
				.Collect(),
			(spc, data) =>
			{
				foreach (var tuple in data)
				{
					if (tuple is not var (methodName, paramName, typeName, @namespace, typeKind, node))
					{
						continue;
					}

					var namespaceString = @namespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
					namespaceString = namespaceString["global::".Length..];

					var typeKindString = typeKind switch
					{
						TypeKind.Class => "class",
						TypeKind.Struct => "struct",
						TypeKind.Interface => "interface",
						_ => throw new InvalidOperationException()
					};

					spc.AddSource(
						$"{typeName}{SourceGeneratorFileNameShortcut.GreetingGenerator_UseAttribute}",
						$$"""
						// <auto-generated/>

						#nullable enable
						namespace {{namespaceString}};

						partial {{typeKindString}} {{typeName}}
						{
							{{node.Modifiers}} void {{methodName}}(string {{paramName}})
								=> global::System.Console.WriteLine($"Hello, {{{paramName}}}!");
						}
						"""
					);
				}
			}
		);
}

file sealed record GatheredData(
	string MethodName,
	string ParameterName,
	string TypeName,
	INamespaceSymbol Namespace,
	TypeKind TypeKind,
	MethodDeclarationSyntax Node
);
