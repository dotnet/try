import { DotNetOnline } from "./interactivity/originalCode";

function setup(global: any) {
    const container = document.querySelector<HTMLDivElement>("div.dotnet-online");
    if (container) {
        const interactive = new DotNetOnline(container);
        if (global) {
            console.log("Setting up dotnetOnline on global");
            global.dotnetOnline = interactive;
        }
    }
}

setup(window);