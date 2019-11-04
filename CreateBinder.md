# Share your notebooks on [Binder](https://mybinder.org/)

###### How to share your .NET Jupyter Notebook 
If you want to share notebooks you have made using .Net kernels one easy way is to generate a binder image that everyone can open and execute.

Requisites: 

* A Github repo and at least one notebook to share
* **Dockerfile** to create the binder image
* **Nuget.Config** file to provide package source to use in notebooks

You can use the Dockerfile and Nuget.Config files from the folder `Binder Dependecies` to get started.

## Steps


The repo file structure should look something like this.

<img src ="https://user-images.githubusercontent.com/375556/67017073-19137180-f0f1-11e9-9744-b5f8ec532e32.png" width = "30%">

The Dockerfile will install dotnet sdk
,then copy the notebooks and Nuget.config to folder under the notebook user

```docker
# Copy notebooks

COPY ./notebooks/ ${HOME}/notebooks/

# Copy package sources

COPY ./NuGet.config ${HOME}/nuget.config

RUN chown -R ${NB_UID} ${HOME}
USER ${USER}
```

Now push your changes to [github](https://github.com/).

Open a browser on [MyBinder homepage](https://mybinder.org/).

<img src ="https://user-images.githubusercontent.com/375556/67016428-16fce300-f0f0-11e9-98e7-d066ecb91049.png" width="70%">

Put your repository url and branch

<img src = "https://user-images.githubusercontent.com/375556/67016633-66dbaa00-f0f0-11e9-8a6d-c7191de3142e.png" width="70%">


Press launch to test your binder.

During development it is useful to use a commit hash so that you can even test different commits at the same time.

When happy with the result expand the section to reveal the link and badge code so you can now embed it in your blogs and posts.

<img src = "https://user-images.githubusercontent.com/375556/67016821-bd48e880-f0f0-11e9-8c79-4fc97a06741a.png" width = "70%">

## Start in jupyter Lab 
Binder will start with jupyter notebook ux, if you want to default to jupyter lab then add `?urlpath=lab` query parameter to the url of your badge.

For example turn

```[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/try/master)```

into 

```[![Binder](https://mybinder.org/badge_logo.svg)](https://mybinder.org/v2/gh/dotnet/try/master?urlpath=lab)```

Return to [README.md](README.md)
