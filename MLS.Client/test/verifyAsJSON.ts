// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as fs from "fs";
import * as util from "util";
import { resolve } from "path";
import * as chai from "chai";

chai.should();

const readFileAsync = util.promisify(fs.readFile);

const testDir = resolve(process.cwd(), "test", "Approvals");

export async function verifyAsJson(filename: string, result: any) {
    let data: any;
    try {
        data = await readFileAsync(resolve(testDir, filename));
    }
    catch (error) {
        throw new Error(`Could not read file ${filename} due to error: ${error}`);
    }
    
    let expectedJson = JSON.parse(data.toString());
    result.should.be.deep.equal(expectedJson);
}
