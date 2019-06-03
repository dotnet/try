// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/// <reference types="chai" />

declare module "chai-exclude" {
  function chaiExclude(chai: any, utils: any): void;

  export = chaiExclude;
}

declare namespace Chai {
  interface Assertion {
    excluding(props: string | string[]): Assertion;
    excludingEvery(props: string | string[]): Assertion;
  }
}
