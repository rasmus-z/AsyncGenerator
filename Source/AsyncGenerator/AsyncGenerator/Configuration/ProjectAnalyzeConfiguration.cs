﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncGenerator.Extensions;
using Microsoft.CodeAnalysis;

namespace AsyncGenerator.Configuration
{
	public delegate IEnumerable<IMethodSymbol> FindAsyncCounterpart(Project project, IMethodSymbol syncMethodSymbol, bool equalParameters, bool searchInheritedTypes);

	public class ProjectAnalyzeConfiguration : IProjectAnalyzeConfiguration
	{
		public Func<IMethodSymbol, MethodConversion> MethodConversionFunction { get; private set; } = m => MethodConversion.Unknown;

		public Func<INamedTypeSymbol, TypeConversion> TypeConversionFunction { get; private set; } = m => TypeConversion.Unknown;

		public Predicate<Document> DocumentSelectionPredicate { get; private set; } = m => true;

		//public Predicate<IMethodSymbol> MethodSelectionPredicate { get; private set; } = m => true;

		//public Predicate<INamedTypeSymbol> TypeSelectionPredicate { get; private set; } = m => true;

		public Predicate<IMethodSymbol> ConvertMethodPredicate { get; private set; } = m => true;

		public List<FindAsyncCounterpart> FindAsyncCounterpartDelegates { get; } = new List<FindAsyncCounterpart>
		{
			(project, symbol, equalParameters, inherit) => symbol.GetAsyncCounterparts(equalParameters, inherit)
		};

		public bool ScanMethodBody { get; private set; }

		public bool ScanForMissingAsyncMembers { get; private set; }

		public ProjectAnalyzeCallbacksConfiguration Callbacks { get; } = new ProjectAnalyzeCallbacksConfiguration();

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.MethodConversion(Func<IMethodSymbol, MethodConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			MethodConversionFunction = func;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.TypeConversion(Func<INamedTypeSymbol, TypeConversion> func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			TypeConversionFunction = func;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.DocumentSelection(Predicate<Document> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			DocumentSelectionPredicate = predicate;
			return this;
		}

		//IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.MethodSelectionPredicate(Predicate<IMethodSymbol> predicate)
		//{
		//	if (predicate == null)
		//	{
		//		throw new ArgumentNullException(nameof(predicate));
		//	}
		//	MethodSelectionPredicate = predicate;
		//	return this;
		//}

		//IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.TypeSelectionPredicate(Predicate<INamedTypeSymbol> predicate)
		//{
		//	if (predicate == null)
		//	{
		//		throw new ArgumentNullException(nameof(predicate));
		//	}
		//	TypeSelectionPredicate = predicate;
		//	return this;
		//}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ConvertMethodPredicate(Predicate<IMethodSymbol> predicate)
		{
			if (predicate == null)
			{
				throw new ArgumentNullException(nameof(predicate));
			}
			ConvertMethodPredicate = predicate;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.AppendFindAsyncCounterpartDelegate(FindAsyncCounterpart func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			FindAsyncCounterpartDelegates.Add(func);
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.PrependFindAsyncCounterpartDelegate(FindAsyncCounterpart func)
		{
			if (func == null)
			{
				throw new ArgumentNullException(nameof(func));
			}
			FindAsyncCounterpartDelegates.Insert(0, func);
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ScanMethodBody(bool value)
		{
			ScanMethodBody = value;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.ScanForMissingAsyncMembers(bool value)
		{
			ScanForMissingAsyncMembers = value;
			return this;
		}

		IProjectAnalyzeConfiguration IProjectAnalyzeConfiguration.Callbacks(Action<IProjectAnalyzeCallbacksConfiguration> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}
			action(Callbacks);
			return this;
		}

	}
}
