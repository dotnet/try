export const LANGUAGE_CSHARP = "csharp";
export const LANGUAGE_FSHARP = "fsharp";

export type SupportedLanguages =
    typeof LANGUAGE_CSHARP | typeof LANGUAGE_FSHARP;

export function isLanguageSupported(language: string): boolean {
    switch (language) {
        case LANGUAGE_CSHARP:
        case LANGUAGE_FSHARP:
            return true;
        default: return false;
    }
}

export function toSupportedLanguage(language:string) : SupportedLanguages{
    if (isLanguageSupported(language)){
        return <SupportedLanguages>language;
    }

    throw new Error(`language ${language} is not supported`);
}

