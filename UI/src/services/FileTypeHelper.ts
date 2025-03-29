class FileTypeHelper{
    private _videoExtensions = [
        "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "3gp", "m4v", "ogv", "mpeg", "mpg", "f4v", "rmvb", "asf", "vob", "mxf", "divx"
    ];
    private _audioExtensions = [
        "mp3", "wav", "aac", "flac", "ogg", "alac", "m4a", "opus", "mid", "midi"
    ];
    private _textExtensions = [
        "txt", "md", "log", "csv", "json", "xml", "yaml", "html", "css", "js", "sh"
    ];
    private _imageExtensions = [
        "jpg", "jpeg", "png", "gif", "bmp", "svg", "ico", "jfif"
    ];
    
    isMedia(fileName: string): boolean{
        return this.isVideo(fileName) || this.isAudio(fileName);
    }
    isFile(fileName: string): boolean{
        return this.isVideo(fileName) || this.isAudio(fileName) || this.isText(fileName) || this.isImage(fileName);
    }
    isVideo(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._videoExtensions.some(z => lower.endsWith("." + z))
    }
    isAudio(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._audioExtensions.some(z => lower.endsWith("." + z))
    }
    isText(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._textExtensions.some(z => lower.endsWith("." + z))
    }
    isImage(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._imageExtensions.some(z => lower.endsWith("." + z))
    }
}
export var fileTypeHelper = new FileTypeHelper()

