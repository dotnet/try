window.BlazorInterop = {
    postMessage: function (message) {

        //console.log("interop posting message: ", message);
        window.postMessage(message, "*");
    }
};


window.addEventListener("message", e => {
    //console.log("Interop js received: ", e);
    DotNet.invokeMethodAsync("MLS.Blazor", "PostMessageAsync", e.data);
});