// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public class DisplayedValue 
    {
        private readonly string _displayId;
        private readonly string _mimeType;

        public DisplayedValue(string displayId, string mimeType)
        {
            if (string.IsNullOrWhiteSpace(displayId))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayId));
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
            }
            _displayId = displayId;
            _mimeType = mimeType;
        }

        public void Update(object updatedValue)
        {
            var formatted = new FormattedValue(
                _mimeType,
                updatedValue.ToDisplayString(_mimeType));

            var kernel = KernelInvocationContext.Current.HandlingKernel;

            Task.Run(() =>
                    kernel.SendAsync(new UpdateDisplayedValue(updatedValue, formatted, _displayId)))
                .Wait();
        }
    }
}