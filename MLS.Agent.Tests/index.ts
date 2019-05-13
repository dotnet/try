import * as path from "path";
import * as fs from "fs";

//Read the expected approval file as json and return the result
export function GetExpectedResultASJSON(filename: string) {
    let testDir = path.resolve(process.cwd(),"node_modules", "mls-agent-results", filename);
    return JSON.parse(fs.readFileSync(testDir).toString());
}