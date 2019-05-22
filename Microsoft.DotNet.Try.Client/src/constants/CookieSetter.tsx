// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CookieAttributes } from "js-cookie";

export type CookieSetter = (name: string, value: string, options?: CookieAttributes) => void;
