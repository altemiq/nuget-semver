// -----------------------------------------------------------------------
// <copyright file="Bind.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Aims.SemanticVersioning;

using System.CommandLine;

/// <summary>
/// Bind methods.
/// </summary>
internal static class Bind
{
    /// <summary>
    /// <see cref="IConsole"/> binder.
    /// </summary>
    public static readonly System.CommandLine.Binding.BinderBase<IConsole> Console = FromServiceProvider<IConsole>();

    /// <summary>
    /// <see cref="CancellationToken"/> binder.
    /// </summary>
    public static readonly System.CommandLine.Binding.BinderBase<CancellationToken> CancellationToken = FromServiceProvider<CancellationToken>();

    /// <summary>
    /// Gets the binder from the service provider.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <returns>The binder.</returns>
    public static System.CommandLine.Binding.BinderBase<T> FromServiceProvider<T>() => ServiceProviderBinder<T>.Instance;

    private sealed class ServiceProviderBinder<T> : System.CommandLine.Binding.BinderBase<T>
    {
        public static ServiceProviderBinder<T> Instance { get; } = new();

        protected override T GetBoundValue(System.CommandLine.Binding.BindingContext bindingContext) => (T)(bindingContext.GetService(typeof(T)) ?? throw new KeyNotFoundException());
    }
}