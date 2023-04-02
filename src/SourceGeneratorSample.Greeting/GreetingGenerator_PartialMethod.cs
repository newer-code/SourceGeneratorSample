﻿namespace SourceGeneratorSample.PartialMethod;

/// <summary>
/// 表示一个源代码生成器，生成一个类型，自带一个打招呼的方法。
/// 这一次用的是分部方法来完成的。分部方法的定义部分由主项目来进行定义，
/// 这里源代码生成器只需要给出它的实现部分，并且方法名称、对应包裹的类型名称只要匹配就可以了。
/// 注意命名空间要保持一致。
/// </summary>
/// <remarks>
/// 虽然说这个源代码生成器生成的内容（打招呼）不是特别有意义，因为这里是举例子，
/// 所以不考虑那么多，只是让各位熟练了解源代码生成器生成的内容，以及如何使用源代码生成器。
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class GreetingGenerator_PartialMethod : ISourceGenerator
{
	/// <inheritdoc/>
	public void Execute(GeneratorExecutionContext context)
	{
		if (context is not
			{
				SyntaxReceiver: SyntaxReceiver
				{
					SayHelloToMethodSyntaxNode: { } methodSyntax
				}
			})
		{
			return;
		}

		var type = methodSyntax.Ancestors().OfType<TypeDeclarationSyntax>().First();
		var typeName = type.Identifier.ValueText;

		context.AddSource(
			$"{typeName}.g.cs",
			$$"""
			// <auto-generated/>

			#nullable enable
			namespace SourceGeneratorSample.Greetings;

			partial class {{typeName}}
			{
				/// <summary>
				/// 和固定的人打招呼。
				/// </summary>
				/// <param name="name">表示对谁打招呼。指定一个字符串，表示其人的名字。</param>
				public static partial void SayHelloTo(string name)
					=> global::System.Console.WriteLine($"Hello, {name}!");
			}
			"""
		);
	}

	/// <inheritdoc/>
	public void Initialize(GeneratorInitializationContext context)
		// 注册一个语法的通知类型。这个类型的作用是为了运行源代码生成器过程之中，
		// 去检查固定语法是否满足条件的这么一个存在。
		=> context.RegisterForSyntaxNotifications(static () => new SyntaxReceiver());
}

/// <summary>
/// 提供一个语法搜索类型。这个类型专门用于寻找主要项目里的指定语法满足条件的部分。
/// </summary>
file sealed class SyntaxReceiver : ISyntaxReceiver
{
	/// <summary>
	/// 表示一个方法的语法节点，这个方法就是我们需要用到的 <c>SayHelloTo</c> 方法。
	/// 它必须是一个静态的方法，而且标记了 <see langword="partial"/> 关键字。
	/// </summary>
	public MethodDeclarationSyntax? SayHelloToMethodSyntaxNode { get; private set; }


	/// <inheritdoc/>
	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		// 检查 syntaxNode 是否满足基本条件：它必须得是一个类型的定义。
		// 它具体是什么类型的类型（class、struct、interface）其实我们并不关心。
		if (syntaxNode is not TypeDeclarationSyntax
			{
				Modifiers: var modifiers and not []
			})
		{
			return;
		}

		// 继续校验，是否类型的修饰符里包含 partial 关键字。
		if (!modifiers.Any(SyntaxKind.PartialKeyword))
		{
			return;
		}

		// 继续校验，这里需要判定它的子节点（类型嵌套内部的这些成员）。
		foreach (var childrenNode in syntaxNode.ChildNodes())
		{
			// 判断当前语法节点是否是一个合理的方法定义。
			if (childrenNode is not MethodDeclarationSyntax
				{
					// 该方法名为 SayHelloTo。
					Identifier.ValueText: "SayHelloTo",

					// 该方法返回一个 void 类型。
					ReturnType: PredefinedTypeSyntax
					{
						Keyword.RawKind: (int)SyntaxKind.VoidKeyword
					},

					// 该方法还需要额外的修饰符（一会儿要用来判断 partial 关键字）。
					Modifiers: var childrenModifiers and not []
				} possibleMethodDeclarationSyntax)
			{
				continue;
			}

			// 该方法必须有 partial 关键字的存在。
			if (!childrenModifiers.Any(SyntaxKind.PartialKeyword))
			{
				continue;
			}

			// 这里是一个补救措施。如果说我们是第一次在项目里找到合适的 SayHelloTo 方法，
			// 我们就把这个节点的基本信息拷贝到 SayHelloToMethodSyntaxNode 属性上去，
			// 提供给后续使用；
			// 如果已经找到过一次后，因为 OnVisitSyntaxNode 这个方法还会继续执行下去，
			// 所以我们需要预先判断 SayHelloToMethodSyntaxNode 属性是否为 null。
			// 如果已经不为 null，就说明已经有一个满足条件的了，这个时候我们就不去管后面的这些了。
			if (SayHelloToMethodSyntaxNode is null)
			{
				SayHelloToMethodSyntaxNode = possibleMethodDeclarationSyntax;
				return;
			}
		}
	}
}
