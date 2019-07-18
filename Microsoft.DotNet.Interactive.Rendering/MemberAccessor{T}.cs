// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Rendering
{
    internal class MemberAccessor<T>
    {
        public MemberAccessor(MemberInfo member)
        {
            var targetParam = Expression.Parameter(typeof(T), "target");

            MemberName = member.Name;

            GetValue = (Func<T, object>)Expression.Lambda(
                typeof(Func<T, object>),
                Expression.TypeAs(
                    Expression.PropertyOrField(targetParam, MemberName),
                    typeof(object)),
                targetParam).Compile();
        }

        public bool Ignore { get; set; }

        public string MemberName { get; }

        public Func<T, object> GetValue { get; set; }
    }
}