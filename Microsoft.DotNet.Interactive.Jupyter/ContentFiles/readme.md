Dotnet try as jupyter kernel
1. If you have installed jupyter via anaconda, open an anaconda command prompt 
2. Execute the following in the command prompt:
		`dotnet try jupyter install`
3. You should see output similar to:
[InstallKernelSpec] Installed kernelspec .net in C:\Users\AppData\Roaming\jupyter\kernels\.net-csharp
[InstallKernelSpec] Installed kernelspec .net in C:\Users\AppData\Roaming\jupyter\kernels\.net-fsharp
.NET kernel installation succeeded
4. Now executing `jupyter kernelspec list` will show the dotnet kernel
	.net-csharp    C:\Users\AppData\Roaming\jupyter\kernels\.net-csharp
	.net-fsharp    C:\Users\AppData\Roaming\jupyter\kernels\.net-fsharp
