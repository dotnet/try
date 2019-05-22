// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

export const groupBy = <A, K>(discriminator: (from: A) => K) => (xs: Array<A>): Map<K, Array<A>> => {
    return xs.reduce((acc, next) => {
        const key = discriminator(next);
        const values = acc.get(key) || [];
        return new Map(acc).set(key, [...values, next]);
    }, new Map())
}

export const flatten = <T>(xs: Array<Array<T>>): Array<T> => xs.reduce((acc, next) => acc.concat(next), []);