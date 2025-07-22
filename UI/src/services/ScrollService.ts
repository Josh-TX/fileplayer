import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import { fileTypeHelper } from "./FileTypeHelper";
import { pathService } from "./PathService";
import { apiAccess } from "./ApiAccess";

type ScrollSpots = {
    y: number,
    range: number,
    path: string
}

class ScrollService{
    _scrollSpots: ScrollSpots[] = []
    _ignoreCount = 0;
    constructor(){
        document.addEventListener("scroll", (event: Event) => {
            if (this._ignoreCount){
                return;
            }
            var path = pathService.getPathString();
            if (fileTypeHelper.isFile(path)){
                return;
            }
            var range = document.documentElement.scrollHeight - window.innerHeight
            var spot = this._scrollSpots.find(z => z.path == path);
            if (spot){
                spot.range = range,
                spot.y = window.scrollY,
                spot.path = path
            } else {
                this._scrollSpots.push({
                    range: range,
                    y: window.scrollY,
                    path: path
                })
            }
        })
    }
    tryScroll(){
        this._ignoreCount = 0;
        var path = pathService.getPathString();
        var spot = this._scrollSpots.find(z => z.path == path);
        var range = document.documentElement.scrollHeight - window.innerHeight
        if (spot && range > 0 && spot.range){
            window.scrollTo({
                top: spot.y,
                behavior: "instant"
            })
        } 
    }
    pathChanged(path: string){
        //filter out all child path spots
        this._scrollSpots = this._scrollSpots.filter(z => z.path.length <= path.length);
        this._ignoreCount++;
        setTimeout(() => {
            this._ignoreCount = Math.max(this._ignoreCount - 1, 0);
        }, 500)
    }
}
export var scrollService = new ScrollService()

