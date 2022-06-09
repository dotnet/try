import { isNullOrUndefinedOrWhitespace } from "./stringExtensions";

export function areSameFile(fileOne: string, fileTwo: string): boolean {
    return fileOne.replace(/\.\//g, "") === fileTwo.replace(/\.\//g, "");
}
export class DocumentId {
    private _relativeFilePath: string;
    private _regionName: string;
    private _stringValue: string;
    toString(): string {
        return this._stringValue;
    }

    constructor(documentId: { relativeFilePath: string, regionName?: string }) {
        this._relativeFilePath = documentId.relativeFilePath;
        this._regionName = documentId.regionName;
        this._stringValue = this._relativeFilePath;
        if (!isNullOrUndefinedOrWhitespace(this._regionName)) {
            this._stringValue = `${this._relativeFilePath}@${this._regionName}`;
        }
    }

    public get relativeFilePath(): string {
        return this._relativeFilePath;
    }

    public get regionName(): string | undefined {
        return this._regionName;
    }

    public static areEqual(a: DocumentId, b: DocumentId): boolean {
        let ret = a === b;
        if (!ret) {
            if (a !== undefined && b !== undefined) {
                ret = a.equal(b);
            }

        }
        return ret;
    }

    public equal(other: DocumentId): boolean {
        return areSameFile(this.relativeFilePath, other.relativeFilePath) && this.regionName === other.regionName;
    }


    public static parse(documentId: string): DocumentId {
        const parts = documentId.split("@");//?
        return parts.length === 1
            ? new DocumentId({ relativeFilePath: parts[0], regionName: parts[1] })
            : new DocumentId({ relativeFilePath: parts[0] });

    }
}
