using System.Reactive.Disposables;
using System.Reflection;

namespace Microsoft.TryDotNet.WasmRunner;

public class CodeRunner 
{
    public Task<RunResults> RunAssemblyEntryPoint(string base64EncodedAssembly, Action<string> onOutput, Action<string> onError)
    {
        var assembly = LoadFromBase64EncodedString(base64EncodedAssembly);
        return RunAssemblyEntryPoint(assembly, onOutput, onError);
    }

    private static  Assembly LoadFromBase64EncodedString(string base64EncodedAssembly)
    {
        if (string.IsNullOrWhiteSpace(base64EncodedAssembly))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(base64EncodedAssembly));
        }
        var bytes = Convert.FromBase64String(base64EncodedAssembly);
        return Assembly.Load(bytes);

    }

    public async Task<RunResults> RunAssemblyEntryPoint(Assembly assembly, Action<string> onOutput,
        Action<string> onError)
    {
        await Task.Yield();

        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        MethodInfo entryPoint;
        var success = false;
        Exception? lastError = null;
        try
        {
            entryPoint = EntryPointDiscoverer.FindStaticEntryMethod(assembly);
        }
        catch (InvalidProgramException ie)
        {
            return new RunResults(success, runnerException: ie);
        }

        using (var consoleState = ConsoleOutput.Subscribe(c =>
               {
                   return new CompositeDisposable
                   {
                       c.Out.Subscribe( output =>
                       {
                           
                           onOutput(output);
                       }),
                       c.Error.Subscribe(error =>
                       {
                           lastError = new Exception(error);
                           onError(error);
                       })
                   };
               }))
        {
            try
            {
                entryPoint.Invoke(null, null);
                success = true;
            }
            catch (Exception e)
            {
                switch (e.InnerException)
                {
                    case TypeLoadException:
                        return new RunResults(success, exception: e, runnerException: e.InnerException);
                    default:
                        return new RunResults(success, exception: e);
                }
               
            }
        }

        success = success && (lastError is null);

        return new RunResults(success,exception:lastError);
    }
}