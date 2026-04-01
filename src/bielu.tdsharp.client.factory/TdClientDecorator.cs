// SPDX-FileCopyrightText: 2024 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

using TdLib;

namespace bielu.tdsharp.client.factory;

/// <summary>
/// Delegate that decorates a <see cref="TdApi.IClient"/> instance, wrapping it with additional behavior.
/// </summary>
/// <param name="inner">The inner client to decorate.</param>
/// <returns>A decorated <see cref="TdApi.IClient"/> instance.</returns>
public delegate TdApi.IClient TdClientDecorator(TdApi.IClient inner);
