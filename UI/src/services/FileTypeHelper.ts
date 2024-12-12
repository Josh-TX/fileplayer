import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";

class FileTypeHelper{
    private _videoExtensions = [
        "mp4", "mkv", "avi", "mov", "wmv", "flv", "webm", "3gp", "m4v", "ogv", "mpeg", "mpg", "f4v", "rmvb", "asf", "vob", "mxf", "divx"
    ];
    private _audioExtensions = [
        "mp3", "wav", "aac", "flac", "ogg", "alac", "m4a", "opus", "mid", "midi"
    ];
    
    isMedia(fileName: string): boolean{
        return this.isVideo(fileName) || this.isAudio(fileName);
    }
    isVideo(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._videoExtensions.some(z => lower.endsWith("." + z))
    }
    isAudio(fileName: string): boolean{
        var lower = fileName.toLowerCase();
        return this._audioExtensions.some(z => lower.endsWith("." + z))
    }
}
export var fileTypeHelper = new FileTypeHelper()

