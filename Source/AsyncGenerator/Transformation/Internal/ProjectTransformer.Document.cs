﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Analyzation;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace AsyncGenerator.Transformation.Internal
{
	partial class ProjectTransformer
	{
		private DocumentTransformationResult TransformDocument(IDocumentAnalyzationResult documentResult)
		{
			var rootNode = documentResult.Node;
			var endOfLineTrivia = rootNode.DescendantTrivia().First(o => o.IsKind(SyntaxKind.EndOfLineTrivia));
			var result = new DocumentTransformationResult(rootNode);
			var rewrittenNodes = new List<TransformationResult>();
			var namespaceNodes = new List<MemberDeclarationSyntax>();
			var hasTaskUsing = rootNode.Usings.Any(o => o.Name.ToString() == "System.Threading.Tasks");
			var hasThreadingUsing = rootNode.Usings.Any(o => o.Name.ToString() == "System.Threading");
			var hasSystemUsing = rootNode.Usings.Any(o => o.Name.ToString() == "System");

			foreach (var namespaceResult in documentResult.Namespaces.OrderBy(o => o.Node.SpanStart))
			{
				var namespaceNode = namespaceResult.Node;
				var typeNodes = new List<MemberDeclarationSyntax>();
				var threadingUsingRequired = false;
				var systemUsingRequired = false;
				foreach (var typeResult in namespaceResult.Types.Where(o => o.Conversion != TypeConversion.Ignore).OrderBy(o => o.Node.SpanStart))
				{
					var transformResult = TransformType(typeResult);
					if (transformResult.TransformedNode == null)
					{
						continue;
					}
					typeNodes.Add(transformResult.TransformedNode);

					threadingUsingRequired |= typeResult.Methods.Any(o => o.CancellationTokenRequired);
					systemUsingRequired |= typeResult.Methods.Any(o => o.WrapInTryCatch);

					// We need to update the original file if it was modified
					if (transformResult.OriginalModifiedNode != null)
					{
						var typeSpanStart = typeResult.Node.SpanStart;
						var typeSpanLength = typeResult.Node.Span.Length;
						var typeNode = rootNode.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
							.First(o => o.SpanStart == typeSpanStart && o.Span.Length == typeSpanLength);
						var rewritenNode = new TransformationResult(typeNode)
						{
							TransformedNode = transformResult.OriginalModifiedNode
						};
						rootNode = rootNode.ReplaceNode(typeNode, typeNode.WithAdditionalAnnotations(new SyntaxAnnotation(rewritenNode.Annotation)));
						rewrittenNodes.Add(rewritenNode);
					}

					// TODO: missing members
					//if (typeInfo.TypeTransformation == TypeTransformation.NewType && typeInfo.HasMissingMembers)
					//{
					//	transformResult = TransformType(typeInfo, true);
					//	if (transformResult.Node == null)
					//	{
					//		continue;
					//	}
					//	typeNodes.Add(transformResult.Node);
					//}
				}
				if (typeNodes.Any())
				{
					var leadingTrivia = namespaceResult.Types.First().Node.GetFirstToken().LeadingTrivia.First(o => o.IsKind(SyntaxKind.WhitespaceTrivia));

					//TODO: check if Task is conflicted inside namespace
					if (!hasTaskUsing && namespaceNode.Usings.All(o => o.Name.ToString() != "System.Threading.Tasks"))
					{
						namespaceNode = namespaceNode.AddUsing("System.Threading.Tasks", TriviaList(leadingTrivia), endOfLineTrivia);
					}
					if (threadingUsingRequired && !hasThreadingUsing && namespaceNode.Usings.All(o => o.Name.ToString() != "System.Threading"))
					{
						namespaceNode = namespaceNode.AddUsing("System.Threading", TriviaList(leadingTrivia), endOfLineTrivia);
					}
					if (systemUsingRequired && !hasSystemUsing && namespaceNode.Usings.All(o => o.Name.ToString() != "System"))
					{
						namespaceNode = namespaceNode.AddUsing("System", TriviaList(leadingTrivia), endOfLineTrivia);
					}
					// TODO: add locking namespaces

					namespaceNodes.Add(namespaceNode
						.WithMembers(List(typeNodes)));
				}
			}
			if (!namespaceNodes.Any())
			{
				return result;
			}
			// Update the original node
			var origRootNode = rootNode;
			foreach (var rewrittenNode in rewrittenNodes)
			{
				origRootNode = rootNode.ReplaceNode(rootNode.GetAnnotatedNodes(rewrittenNode.Annotation).First(), rewrittenNode.TransformedNode);
			}
			if (rootNode != origRootNode)
			{
				result.OriginalModifiedNode = origRootNode;
			}

			// Create the new node
			rootNode = rootNode
					.WithMembers(List(namespaceNodes));
			// Add auto-generated comment
			var token = rootNode.DescendantTokens().First();
			rootNode = rootNode.ReplaceToken(token, token.AddAutoGeneratedTrivia(endOfLineTrivia));

			result.TransformedNode = rootNode;
			return result;
		}
	}
}
