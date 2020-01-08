// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    internal static class KernelHierarchy
    {
        static ConcurrentDictionary<IKernel, List<IKernel>> _parentChild = new ConcurrentDictionary<IKernel, List<IKernel>>();
        static ConcurrentDictionary<IKernel,IKernel> _childParent = new ConcurrentDictionary<IKernel, IKernel>();

        public static void AddChildKernel(IKernel parent, IKernel childKernel)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (childKernel == null)
            {
                throw new ArgumentNullException(nameof(childKernel));
            }

            if (_childParent.TryGetValue(childKernel, out var currentParent) && currentParent != parent)
            {
                throw new InvalidOperationException(
                    $"kernel {childKernel.Name} is already parented to {_childParent[childKernel].Name}");
            } 
            
            if (_childParent.TryAdd(childKernel,parent))

            {
                _childParent[childKernel] = parent;

                _parentChild.AddOrUpdate(parent,
                    kernel => new List<IKernel> {childKernel},
                    (_, list) =>
                    {
                        list.Add(childKernel);
                        return list;
                    });
            }
        }

        public static IKernel GetParent(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return _childParent.TryGetValue(kernel, out var parent) ? parent : null;
        }

        public static IEnumerable<IKernel> GetChildren(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            return _parentChild.TryGetValue(kernel, out var kernels) ? kernels : Enumerable.Empty<IKernel>();
        }

        public static void DeleteNode(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            if (_parentChild.TryRemove(kernel, out var children))
            {
                foreach (var child in children)
                {
                    _childParent.Remove(child, out _);
                }
            }
        }
    }
}