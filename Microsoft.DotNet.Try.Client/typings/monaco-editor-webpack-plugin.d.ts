// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

declare global {
    namespace Chai {
        interface Assertion {
            excluding(props: string | string[]): Assertion;
            excludingEvery(props: string | string[]): Assertion;
        }
        interface Assert {
            /**
             * Asserts that actual is deeply equal to expected excluding some top level properties.
             *
             * @type T          Type of the objects.
             * @param actual    Actual value.
             * @param expected  Potential expected value.
             * @param props     Properties or keys to exclude.
             * @param message   Message to display on error.
             */
            deepEqualExcluding<T>(actual: T, expected: T, props: string | string[], message?: string): void;

            /**
             * Asserts that actual is deeply equal to expected excluding properties any level deep.
             *
             * @type T          Type of the objects.
             * @param actual    Actual value.
             * @param expected  Potential expected value.
             * @param props     Properties or keys to exclude.
             * @param message   Message to display on error.
             */
            deepEqualExcludingEvery<T>(actual: T, expected: T, props: string | string[], message?: string): void;
        }
    }
}

declare function chaiExclude(chai: any, utils: any): void;
export = chaiExclude;
