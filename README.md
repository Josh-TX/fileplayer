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

The volume mount for `/data` is the root path where the media will be stored, and therefore is very important. The `/metadata` path is where the settings & playback history/progress of media is stored. A volume mount for `/metadata` is only recommended insofar as you want the playback history/progress persisted past the lifespan of the container. 

# Notes

The server stores the playback progress within the `/metadata` folder. In order to know which metadata-entry goes to which file, it creates a hash using the file's size (in bytes) and the file's name. The file's parent folder is NOT part of the hash, so you can move files to different folders and the progress is still remembered. When you rename a file through the UI, the server will update the metadata's hash. But renaming files outside of the app will cause playback progress to be lost. One problem with this approach is that if you copy a file and then rename the copy, the original file will have its progress lost.

There's a hidden feature for specifying a specific playback speed for all media within a folder. If your folder name includes something like [1.5x] or [2X], then the media will use that playback speed rather than whatever your last playback speed was. This can be useful if you have 2 different podcast folders, and one podcast you prefer 1.5x speed, and the other you prefer 2x speed. The syntax has to include the square backets `[` and `]` with no spaces (the actual regex is `/\[(\d{1,2}(\.\d+)?)x\]/gi`). This feature allows having playback speeds more extreme then normally allowed through the browser's native media player. Though Chrome seems to not support speeds faster than 16x.
