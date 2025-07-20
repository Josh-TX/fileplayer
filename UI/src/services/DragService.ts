import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import { fileTypeHelper } from "./FileTypeHelper";
import { pathService } from "./PathService";
import { apiAccess } from "./ApiAccess";

type fileCallback = (files: File[]) => any
type moveCallback = () => any

class DragService{
    _fileSubscriptions: fileCallback[] = []
    _moveSubscriptions: moveCallback[] = []
    constructor(){
        
    }
    subscribeFile(callback: fileCallback){
        this._fileSubscriptions.push(callback)
    }
    unsubscribeFile(callback: fileCallback){
        this._fileSubscriptions = this._fileSubscriptions.filter(z => z != callback);
    }
    subscribeMove(callback: moveCallback){
        this._moveSubscriptions.push(callback)
    }
    unsubscribeMove(callback: moveCallback){
        this._moveSubscriptions = this._moveSubscriptions.filter(z => z != callback);
    }

    isDragging = ref(false);
    dragEnter(event: DragEvent) {
        event.preventDefault();
        if (!event.dataTransfer || !event.dataTransfer.files || !event.dataTransfer.files.length){
            return;
        }
        this.isDragging.value = true;
    };
    dragLeave(event: DragEvent) {
        event.preventDefault();
        var relatedTarget = event.relatedTarget //|| event.fromElement;
        var dropArea = document.getElementById("app-main");
        if (!dropArea || !dropArea!.contains(<any>relatedTarget)) {
            this.isDragging.value = false;
        }
    };
    
    handleDrop(event: DragEvent){
        const files = event.dataTransfer?.files;
        if (files && files.length) {
            for(var subscription of this._fileSubscriptions){
                subscription(Array.from(files));
            }
            this.isDragging.value = false;
        } else if (event.target) {
            const draggedHref = event.dataTransfer?.getData('text/plain');
            if (!draggedHref || !draggedHref.startsWith("http") || !draggedHref.includes("#")){
                return;
            }
            const draggedAnchor = <HTMLAnchorElement>document.querySelector(`a[href="${CSS.escape(draggedHref)}"]`);
            const dropAnchor = (<HTMLAnchorElement>event.target).closest("a");
            if (!draggedAnchor || !dropAnchor || !dropAnchor.href.includes("#")){
                return;
            }
            var dropUrl = decodeURIComponent(dropAnchor.href.substring(dropAnchor.href.indexOf("#") + 1));
            var draggedUrl = decodeURIComponent(draggedHref.substring(draggedHref.indexOf("#") + 1));
            if (fileTypeHelper.isFile(dropUrl)){
                console.log("dropped on a file. returning");
                return;
            }
            var currentPath = pathService.getPathString();
            if (!dropUrl.startsWith(currentPath) || !draggedUrl.startsWith(currentPath)){
                console.log("at least 1 url isn't relative to current path")
            }
            var commonLength = currentPath == "" ? 0 : currentPath.length + 1;
            var response = confirm(`Move ${draggedUrl.substring(commonLength)} to ${dropUrl.substring(commonLength)}?`)
            if (response){
                console.log(dropUrl, draggedUrl)
                apiAccess.copyItems({
                    isMove: true,
                    filePaths: [draggedUrl],
                    destinationDir: dropUrl
                }).then(z => {
                    for(var subscription of this._moveSubscriptions){
                        subscription();
                    }
                })
            }
        }
    };

}
export var dragService = new DragService()

