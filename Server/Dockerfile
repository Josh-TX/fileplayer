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

RUN apt-get update \
&& apt-get install -y wget python3 unzip \
&& wget -qO /usr/local/bin/yt-dlp https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
&& chmod a+rx /usr/local/bin/yt-dlp \
&& wget -qO /tmp/ffmpeg.zip https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v6.1/ffmpeg-6.1-linux-64.zip \
&& unzip -o /tmp/ffmpeg.zip -d /usr/local/bin \
&& chmod a+rx /usr/local/bin/ffmpeg \
&& apt-get purge -y --auto-remove wget unzip \
&& apt-get clean \
&& rm -rf /var/lib/apt/lists/* \
&& mkdir /data \
&& mkdir /metadata
WORKDIR /app
COPY --from=dotnet-build /app .
ENTRYPOINT ["dotnet", "./fileplayer.dll"]