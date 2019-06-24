var dotnetInstance;

window.BlazorInterop = {
    postMessage: function (message) {

        //console.log("interop posting message: ", message);
        window.postMessage(message, "*");
    },

    install: function (obj) {
        console.log("dotnet installed");
        dotnetInstance = obj;
    }
};


window.addEventListener("message", e => {
    console.log("Interop js received: ", e);
    dotnetInstance.invokeMethodAsync("MLS.Blazor.PostMessageAsync", e.data);
});