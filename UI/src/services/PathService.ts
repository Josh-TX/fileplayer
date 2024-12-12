import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import { fileTypeHelper } from "./FileTypeHelper";

class PathService{
    private _path = shallowRef<string[]>([]);
    private _isFile = ref<boolean>(false);
    private mediaInfo: MediaInfo | null = null;
    private _selfNavigated: boolean = false;

    constructor(){
        var hash = decodeURIComponent(window.location.hash.slice(1));
        if (hash){
            this._path.value = hash.split("/");
        }
        if (!this._path.value.length){
            this._isFile.value = false;
        } else {
            var lastPart = this._path.value[this._path.value.length-1].toLowerCase();
            if (fileTypeHelper.isMedia(lastPart)){
                this._isFile.value = true;
            }
        }
        window.addEventListener('popstate', () => {
            var hash = decodeURIComponent(window.location.hash.slice(1));
            this._path.value = hash ? hash.split("/") : [];
            if (!this._selfNavigated){
                this._isFile.value = this._path.value.length ? this._path.value[this._path.value.length-1].includes(".") : false;
            }
        });
    }



    appendFolder(folderName: string){
        this._isFile.value = false;
        this.mediaInfo = null;
        this._path.value = [...this._path.value, folderName];
        this.updateHash();
    }

    navigate(index: number){
        this._isFile.value = false; //since navigate only goes up the hiarachy, it can't be a file
        this.mediaInfo = null;
        this._path.value = this._path.value.slice(0, index);
        this.updateHash();
    }

    appendFile(mediaInfo: MediaInfo){
        this._isFile.value = true;
        this.mediaInfo = mediaInfo;
        this._path.value = [...this._path.value, mediaInfo.fileName];
        this.updateHash();
    }

    isFile(): Ref<boolean>{
        return this._isFile;
    }

    getMediaInfo(): MediaInfo | null{
        return this.mediaInfo;
    }

    getPathString(){
        return this._path.value.join("/");
    }

    getPath(){
        return this._path;
    }

    private updateHash(){
        var pathString = this.getPathString();
        var encodedPath = encodeURIComponent(pathString);
        this._selfNavigated = true;
        window.location.hash = encodedPath;
        setTimeout(() => this._selfNavigated = false, 200);
    }

    private updateIsFile(){

    }
}
export var pathService = new PathService()

