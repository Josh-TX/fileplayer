# Fileplayer
a web server for uploading, browsing, and playing media files. Intended for podcasts and other long-form media

* Upload media via file upload, or provide a url and the server will use yt-dlp to download media in the background
* Browse files by folder. Each media file has a progress bar to indicate what percentage you've watched/listened. 
* When playing media, it automatically starts where you left off, and it remembers your playback speed

# Installation

The server can be deployed via docker with the following command

```
docker run -p 8080:8080 -v /path/to/data:/data -v /path/to/metadata:/metadata joshtxdev/fileplayer
```

The volume mount for `/data` is the root path where the media will be stored, and therefore is very important. The `/metadata` path is where the settings & playback history/progress of media is stored. A volume mount for `/metadata` is only recommended insofar as you want the playback history persisted past the lifespan of the container. 