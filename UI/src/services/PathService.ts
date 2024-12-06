import { ref, type Ref, shallowRef } from "vue";

class PathService{
    private _path = shallowRef<string[]>([]);
    private _isFile = ref<boolean | null>(null);

    constructor(){
        console.log("constructor pathservice")
        var hash = decodeURIComponent(window.location.hash.slice(1));
        console.log("hash", hash)
        if (hash){
            this._path.value = hash.split("/");
        }
        if (!this._path.value.length){
            this._isFile.value = false;
        }
        window.addEventListener('popstate', () => {
            var hash = decodeURIComponent(window.location.hash.slice(1));
            this._path.value = hash ? hash.split("/") : [];
            this._isFile.value = this._path.value.length ? this._path.value[this._path.value.length-1].includes(".") : false;
        });
    }

    appendFolder(folderName: string){
        this._isFile.value = false;
        this._path.value = [...this._path.value, folderName];
        this.updateHash();
    }

    navigate(index: number){
        this._isFile.value = false;
        this._path.value = this._path.value.slice(0, index);
        this.updateHash();
    }

    appendFile(fileName: string){
        this._isFile.value = true;
        this._path.value = [...this._path.value, fileName];
        this.updateHash();
    }

    isFile(): Ref<boolean | null>{
        return this._isFile;
    }

    getPathString(){
        return this._path.value.join("/");
    }

    getWatcherPathRef(){
        return this._path;
    }

    private updateHash(){
        var pathString = this.getPathString();
        var encodedPath = encodeURIComponent(pathString);
        window.location.hash = encodedPath;
        console.log("updateHash", pathString)
    }

    private updateIsFile(){

    }
}
export var pathService = new PathService()

