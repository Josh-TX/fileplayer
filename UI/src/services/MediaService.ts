import { ref, type Ref, shallowRef } from "vue";
import { apiAccess } from '@/services/ApiAccess';
import type { MediaInfo } from "@/models/models";


class MediaService{
    private _path: string | null = null;
    private _lastProgress: number | null = null;
    private _lastUpdateTime: number | null = null;

    constructor(){
        setInterval(() => {
            if (this._path){
                var el = <HTMLVideoElement>document.getElementById('media');
                if (el){
                    const currentProgress = (el.currentTime / el.duration);
                    if (this._lastProgress == null){
                        if (currentProgress > 0){
                            this.updateProgress(el);//we went from never watched to partially watched
                        }
                        return;
                    }
                    if (this._lastProgress == 1 && currentProgress == 0){
                        return; //we loaded the player already finished and haven't played yet
                    }
                    if (this._lastProgress == currentProgress){
                        return; //progress is unchanged
                    }
                    if (this._lastProgress != 1 && currentProgress == 1){
                        this.updateProgress(el); //the video just went from not finished to finished
                        return;
                    }
                    if (currentProgress < this._lastProgress){
                        this.updateProgress(el); //the video was seeked backwards
                        return;
                    }
                    if (this._lastUpdateTime == null){
                        this.updateProgress(el); //first update this session
                        return;
                    }
                    var elapsed = (currentProgress - this._lastProgress) * el.duration;
                    if (elapsed > 4){
                        this.updateProgress(el); //video is over 4 seconds ahead of the last update
                        return;
                    }
                } else {
                    this.tearDown();
                }
            }
        }, 250)
    }

    updateProgress(el: HTMLVideoElement){
        this._lastProgress = (el.currentTime / el.duration);
        this._lastUpdateTime = new Date().getTime();
        apiAccess.updateProgress(this._lastProgress , this._path!);
    }

    tearDown(){
        this._path = null;
    }

    setup(path: string, progress: number | null){
        this._path = path;
        var el = <HTMLVideoElement>document.getElementById('media');
        if (!el){
            throw "couldn't find media element";
        }
        el.playbackRate = 2;
        el.addEventListener('play', () => {
            if (this.isSafariMobile()){
                el.currentTime = 10;
            }
        });
        el.addEventListener('pause', () => {

        });
        el.addEventListener('ended', () => {

        });
        this._lastProgress = progress;
        if (progress && progress != 1){
            if (el.readyState > 1){
                el.currentTime = el.duration * progress;
                this._lastProgress = (el.currentTime / el.duration)
            } else {
                el.addEventListener('loadedmetadata', () => {
                    if (!this.isSafariMobile()){
                        el.currentTime = el.duration * progress;
                        this._lastProgress = (el.currentTime / el.duration)
                    }
                });
            }
        }
    }

    private isSafariMobile() {
        const userAgent = navigator.userAgent;
        return /iP(hone|od|ad)/.test(userAgent) && /Safari/.test(userAgent) && !/Chrome/.test(userAgent);
    }
}
export var mediaService = new MediaService()

