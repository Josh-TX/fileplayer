import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import { fileTypeHelper } from "./FileTypeHelper";

class PathService{
    private _path = shallowRef<string[]>([]);
    private _baseUrl = "";
    private _isFile = ref<boolean>(false);

    constructor(){
        this._baseUrl = window.location.href.split('#')[0]!;
        var hash = decodeURIComponent(window.location.hash.slice(1));
        if (hash){
            this._path.value = hash.split("/");
        }
        if (!this._path.value.length){
            this._isFile.value = false;
        } else {
            var lastPart = this._path.value[this._path.value.length-1].toLowerCase();
            if (fileTypeHelper.isFile(lastPart)){
                this._isFile.value = true;
            }
        }
        window.addEventListener('popstate', () => {
            var hash = decodeURIComponent(window.location.hash.slice(1));
            this._path.value = hash ? hash.split("/") : [];
            this._isFile.value = this._path.value.length ? this._path.value[this._path.value.length-1].includes(".") : false;
            if (!this._isFile.value){
                document.title = "Fileplayer";
            }
        });
    }

    setIsFile(): void{
        this._isFile.value = true;
    }

    isFile(): Ref<boolean>{
        return this._isFile;
    }

    getPathString(){
        return this._path.value.join("/");
    }

    getPath(){
        return this._path;
    }

    getTrimmedUrl(index: number){
        if (index == 0){
            return this._baseUrl + "#";
        }
        var pathStr = this._path.value.slice(0, index).join("/");
        return this._baseUrl + "#" + encodeURIComponent(pathStr);
    }

    getAppendedUrl(appendedPath: string){
        if (!this._path.value.length){
            return this._baseUrl + "#" + encodeURIComponent(appendedPath);
        }
        return window.location + "%2F" + encodeURIComponent(appendedPath);
    }
}
export var pathService = new PathService()

