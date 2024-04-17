FROM mcr.microsoft.com/dotnet/sdk:8.0-cbl-mariner2.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./

# Make sure we run bash
CMD ["bash"]

# Make sure we get all the updates and tools we need to build
RUN tdnf install gawk -y
# This is Node v16.  For 18, use nodejs18.
RUN tdnf install nodejs -y
RUN tdnf clean all

# Build javascript library
RUN /App/build-js.sh

# Restore
RUN dotnet restore --configfile /App/NuGet.config /App/TryDotNet.sln

# Build and publish a release
RUN dotnet publish -c Release -o out /App/src/Microsoft.TryDotNet

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0-cbl-mariner2.0
ARG TRY_DOT_NET_BUILD_ID
WORKDIR /App

# Make sure we run bash
CMD ["bash"]

# Make sure we get all the tools we need
RUN tdnf install procps -y
RUN tdnf clean all

# Copy from build image
COPY --from=build-env /App/out .

# Set up to run and expose app on port 80
EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80/

# This is a workaround for the fact that the Try .NET website is not yet container-aware
ENV TRY_DOT_NET_REQUEST_SCHEME=https
ENV TRY_DOT_NET_BUILD_ID=$TRY_DOT_NET_BUILD_ID
ENV TRY_DOT_NET_MANUAL_BUILD_ID=2 

# Run the Microsoft.TryDotNet website
ENTRYPOINT ["dotnet", "Microsoft.TryDotNet.dll"]
