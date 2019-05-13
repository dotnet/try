// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace BasicConsole
{
    class Cat
    {
        public string Say() 
        {
            #region WhatToSay
            var text = "meow! meow!";
            return text[^5..^0];
            #endregion
        }
    }
}
