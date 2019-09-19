using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Rendering;
using System;
using System.Threading.Tasks;

namespace TraceObservablesKernelExtension
{
    public class TraceObservablesKernelExtension : IKernelExtension
    {
        public async Task OnLoadAsync(IKernel kernel)
        {
            var assemblyLocation = typeof(TraceObservablesKernelExtension).Assembly.Location;
            await kernel.SendAsync(new SubmitCode($"#r \"{assemblyLocation}\""));
            await kernel.SendAsync(new SubmitCode($"using static {typeof(TraceObservablesKernelExtension).FullName};"));
            await kernel.SendAsync(new SubmitCode($@"Console.WriteLine(""Extension loaded. You can now call Trace() method on any observable"");"));
        }

        public static async Task Trace<T>(IObservable<T> observable, string mimeType = HtmlFormatter.MimeType)
        {
            var kernel = KernelInvocationContext.Current.HandlingKernel;

            var displayId = Guid.NewGuid().ToString();
            var value = "displaying observable";
            await Display(kernel, value, mimeType, displayId);

            var subscription = observable.Subscribe(async e =>
            {
                await UpdateDisplay(kernel, e, mimeType, displayId);
            });

            subscription.Dispose();
            await Display(kernel, "Execution completed", mimeType);
        }

        private static async Task Display(IKernel kernel, object value, string mimeType, string displayId = null)
        {
            var formatted = new FormattedValue(
           mimeType,
           value.ToDisplayString(mimeType));

            await kernel.SendAsync(new DisplayValue(value, formatted, displayId));
        }

        private static async Task UpdateDisplay(IKernel kernel, object value, string mimeType, string displayId)
        {
            var formatted = new FormattedValue(
           mimeType,
           value.ToDisplayString(mimeType));

            await kernel.SendAsync(new UpdateDisplayedValue(value, formatted, displayId));
        }
    }
}
