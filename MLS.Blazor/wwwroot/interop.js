var dotnetInstance;

window.BlazorInterop = {
    postMessage: function (message) {

        console.log("interop posting message: ", message);
        window.postMessage(message, "*");
    },

    log: function (message) {
        console.log(message);
    },

    install: function (obj) {
        console.log("dotnet installed");
        dotnetInstance = obj;
    }
};


window.addEventListener("message", e => {

        dotnetInstance.invokeMethodAsync("MLS.Blazor.PostMessageAsync", e.data)
            .then(r => {
                if (r !== null) {
                    window.postMessage(r, "*");
                }
            });

    
});