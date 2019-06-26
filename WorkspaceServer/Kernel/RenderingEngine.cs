// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkspaceServer.Kernel
{

    public class RenderingEngine : IRenderingEngine
    {
        private readonly IRenderer _defaultRenderer;
        private readonly Dictionary<Type,IRenderer> _rendererRegistry = new Dictionary<Type, IRenderer>();

        public RenderingEngine(IRenderer defaultRenderer)
        {
            _defaultRenderer = defaultRenderer;
        }
        public IRendering Render(object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            var renderer = FindRenderer(source.GetType());
            return renderer.Render(source, this);
        }

        public IRenderer FindRenderer(Type sourceType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }
            return _rendererRegistry.TryGetValue(sourceType, out var renderer) ? renderer : FindMatchingRenderer(sourceType);
        }

        private IRenderer FindMatchingRenderer(Type sourceType)
        {
            var keyCandidates = new List<Type>();

            var candidates = _rendererRegistry.Keys.Where(key => key.IsAssignableFrom(sourceType)).ToList();

            if (candidates.Count == 0)
            {
                return _defaultRenderer;
            }

            foreach (var candidate in candidates)
            {
                var found = false;

                for (var i = 0; i < keyCandidates.Count; i++)
                {
                    var current = keyCandidates[i];
                    if (current.IsAssignableFrom(candidate))
                    {
                        keyCandidates[i] = candidate;
                        found = true;
                    }
                    else if (candidate.IsAssignableFrom(current))
                    {
                        found = true;
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (!found)
                {
                    keyCandidates.Add(candidate);
                }
            }

            return _rendererRegistry[keyCandidates[0]];
        }

        public void RegisterRenderer(Type sourceType, IRenderer renderer)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            _rendererRegistry[sourceType] = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public IRenderer FindRenderer<T>()
        {
            return FindRenderer(typeof(T));
        }

        public void RegisterRenderer<T>(IRenderer<T> renderer)
        {
           RegisterRenderer(typeof(T), renderer);
        }

        public void RegisterRenderer<T>(IRenderer renderer)
        {
            RegisterRenderer(typeof(T), renderer);
        }
    }
}