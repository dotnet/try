# Create .NET Jupyter Notebooks
##### Getting started locally on your machine 

### Installation 

Requirements
- [.NET 3.0 SDK](https://dotnet.microsoft.com/download) (follow the setup for your OS)
- [Jupyter](https://jupyter.org/install) : JupyterLab can be installed using [Anaconda](https://www.anaconda.com/distribution) or  `conda` or `pip`. For more details on how to do this please checkout the [offical Jupyter installation](https://jupyter.org/install) guide.

### Install the .NET Kernel
- Open Command Prompt or if you have Anaconda installed use Anaconda Prompt
- Install the dotnet try global tool

    `dotnet tool install -g dotnet-try --add-source https://dotnet.myget.org/F/dotnet-try/api/v3/index.json`

*Please note: If you have the `dotnet try` global tool already installed, you will need to uninstall before grabbing the kernel enabled version of the dotnet try global tool.*
- Check to see if jupyter is installed 

    `jupyter kernelspec list`
    
- Install the kernel 

    `dotnet try jupyter install`

    <img src ="https://user-images.githubusercontent.com/2546640/63954737-93106e00-ca51-11e9-8c72-939f3f558d05.png" width = "50%">
    
- Test installation 

    `jupyter kernelspec list`

    You should see the `.net-csharp`  and `.net-fsharp` listed.
    
    <img src ="https://user-images.githubusercontent.com/2546640/67889556-76fa7d00-fb25-11e9-9d23-e4178642b721.png" width ="70%">

* To start a new notebook, you can either type `jupyter lab`  Anaconda prompt or launch a notebook using the Anaconda Navigator.
* Once Jupyter Lab has launched in your preferred browser, you have the option to create a **C# and F# notebook**.

    <img src = "https://user-images.githubusercontent.com/2546640/67889988-3b13e780-fb26-11e9-91a1-48d5972b5df2.png" width = "70%">

-  Now you can write .NET 

    <img src = "https://user-images.githubusercontent.com/2546640/67981834-db860c80-fbf7-11e9-89b5-29d2480ed1fa.png" width = "70%">

For more information on our APIs via C# and F#, please check out our documentation on [binder](https://mybinder.org/v2/gh/dotnet/try/master?urlpath=lab) or in dotnet/try repo in the NotebookExamples folder.

<img src = "https://user-images.githubusercontent.com/2546640/67980555-120e5800-fbf5-11e9-9c00-0d021b1ed21c.png" width = "40%">

 Now that you have created your .NET notebook, you probably want to share it with others. In the [next document](CreateBinder.md), you will learn how to share your .NET notebook with others using binder. 

 Happy sharing! 
