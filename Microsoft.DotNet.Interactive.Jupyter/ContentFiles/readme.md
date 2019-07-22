Dotnet try as jupyter kernel



Create a folder called `.NET` in the kernels forlder of you jupyter installation, if using Anaconda3 on windows it is `%localAppData%\Continuum\anaconda3\share\jupyter\kernels`.

Copy the `kernel.json` file and the two incons there.

Now jupyter will list dotnet as an available kernel, to test open a terminal in an Anaconda environment and type `jupyter kernelspec list` it should show dotnet in the list.