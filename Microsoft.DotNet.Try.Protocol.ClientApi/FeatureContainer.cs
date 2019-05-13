// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Pocket;

namespace Microsoft.DotNet.Try.Protocol.ClientApi
{
    public abstract class FeatureContainer : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private readonly Dictionary<string, object> _features =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private List<(string, object)> _featureProperties;


        public IReadOnlyDictionary<string, object> Features => _features;

        public void Dispose() => _disposables.Dispose();

        public void AddFeature(IFeature feature)
        {
            if (feature is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }

            _features.Add(feature.Name, feature);
        }

        public List<(string Name, object Value)> FeatureProperties => _featureProperties ?? (_featureProperties = new List<(string, object)>());

        public void AddProperty(string name, object value) => FeatureProperties.Add((name, value));
    }
}