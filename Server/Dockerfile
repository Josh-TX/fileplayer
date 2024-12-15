# This dockerfile assumes that /Server/wwwroot/ already contains the built UI
# Stage 1 - Publish dotnet 
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build

ARG TARGETARCH
WORKDIR /source
COPY fileplayer.csproj ./
RUN dotnet restore
COPY . .
RUN dotnet publish -a $TARGETARCH -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0


RUN apt-get update && apt-get install -y wget
RUN wget -qO /usr/local/bin/yt-dlp https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp && chmod a+rx /usr/local/bin/yt-dlp
RUN apt-get update && apt-get install -y ffmpeg && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=dotnet-build /app .
RUN mkdir /data
RUN mkdir /metadata
ENTRYPOINT ["./fileplayer"]
