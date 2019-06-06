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

export function toSupportedLanguage(langauge:string) : SupportedLanguages{
    if (isLanguageSupported(langauge)){
        return <SupportedLanguages>langauge;
    }

    throw new Error(`language ${langauge} is not supported`);
}

