#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80

RUN apt update
RUN apt install -y wget
RUN wget https://mozjpeg.codelove.de/bin/mozjpeg_3.3.1_amd64.deb
RUN dpkg -i mozjpeg_3.3.1_amd64.deb

RUN apt install -y libpng16-16

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["XPY.MozJPEGService/XPY.MozJPEGService.csproj", "XPY.MozJPEGService/"]
RUN dotnet restore "XPY.MozJPEGService/XPY.MozJPEGService.csproj"
COPY . .
WORKDIR "/src/XPY.MozJPEGService"
RUN dotnet build "XPY.MozJPEGService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "XPY.MozJPEGService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "XPY.MozJPEGService.dll"]