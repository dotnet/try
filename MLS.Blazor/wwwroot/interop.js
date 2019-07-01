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


var seq = -1;
window.addEventListener("message", e => {
    console.log("Interop js received: ", e);
    console.log(e.data);

    if (e.data.Sequence !== undefined && e.data.Sequence > seq) {
        seq = e.data.Sequence;
        dotnetInstance.invokeMethodAsync("MLS.Blazor.PostMessageAsync", e.data)
            .then(r => {
                console.log(r);
                window.postMessage(r, "*");
            });
    }

    
});