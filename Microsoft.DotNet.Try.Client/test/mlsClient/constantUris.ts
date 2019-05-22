// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const baseAddress: URL = new URL("http://localhost:27261");
export const uriThatENOTFOUNDs: URL = new URL("https://ENOTFOUND.com");
export const uriThatECONNREFUSEDs: URL = new URL("https://127.127.127.127");
export const uriThat404s: URL = new URL("https://try.a404.net/");