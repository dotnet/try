// * Global

/**
 * The globally available descriptor of the current theme. It is updated each time setTheme is called.
 */
 export let currentTheme: ThemeType = 'light';

 // * Types
 
 export type ThemeType = keyof ThemeTypeMap;
 
 export interface ThemeTypeInfo {
     documentClass: string;
     name: string;
     text: string;
     icon: string;
 }
 
 export interface ThemeTypeMap {
     light: ThemeTypeInfo;
     dark: ThemeTypeInfo;
     'high-contrast': ThemeTypeInfo;
 }
 

 // * Events

export interface ThemeChangeInfo {
	currentTheme: ThemeType;
	previousTheme: ThemeType;
}

/**
 * Global Event that fires when the visual theme is changed.
 * @param currentTheme A string descriptor of the current theme (the theme just applied).
 * @param previousTheme A string descriptor of the previous theme.
 */
export class ThemeChangedEvent implements ThemeChangeInfo {
	constructor(public readonly currentTheme: ThemeType, public readonly previousTheme: ThemeType) {}
}