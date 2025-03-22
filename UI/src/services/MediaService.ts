import { ref, type Ref, shallowRef } from "vue";
import { apiAccess } from '@/services/ApiAccess';
import { settingsService } from '@/services/SettingsService';
import type { MediaInfo } from "@/models/models";


class MediaService{
    private _path: string | null = null;
    private _lastProgress: number | null = null;
    private _lastUpdateTime: number | null = null;
    private _lastPlaybackRate: number = 1;
    private _staticPlaybackRate: number | null = null;
    constructor(){
        setInterval(() => {
            if (this._path){
                var el = <HTMLVideoElement>document.getElementById('media');
                if (el){
                    if (el.playbackRate != this._lastPlaybackRate && this._staticPlaybackRate == null){
                        //only update the settings when the path does NOT have a static playbackRate
                        settingsService.updatePlaybackSpeed(el.playbackRate);
                        this._lastPlaybackRate = el.playbackRate
                    }
                    const currentProgress = (el.currentTime / el.duration);
                    if (this._lastProgress == null){
                        if (currentProgress > 0){
                            this.updateProgress(el);//we went from never watched to partially watched
                        }
                        return;
                    }
                    if (this._lastProgress == 1 && currentProgress == 0){
                        return; //we loaded the player positioned at the end and haven't played yet
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
                    var videoSecondsElapsed = (currentProgress - this._lastProgress) * el.duration;
                    if (videoSecondsElapsed > 8){
                        this.updateProgress(el); //video is over 8 seconds ahead of the last update
                        return;
                    }
                    var timeSinceLastUpdate = ((new Date().getTime() - this._lastUpdateTime)) / 1000;
                    if (timeSinceLastUpdate > 4){
                        this.updateProgress(el); //It's been 4 seconds since the last update
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
        this._staticPlaybackRate = this.extractStaticSpeed(path);
        var el = <HTMLVideoElement>document.getElementById('media');
        if (!el){
            throw "couldn't find media element";
        }
        if (this._staticPlaybackRate != null){
            el.playbackRate = this._staticPlaybackRate
        } else {
            settingsService.getSettingsAsync().then(z => {
                el.playbackRate = z.playbackSpeed
                this._lastPlaybackRate = z.playbackSpeed;
            });
        }
        el.addEventListener('play', () => {
            if (this.isSafariMobile()){
                //might need to do something here
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

    private extractStaticSpeed(path: string): number | null{
        const matches = path.match(/\[(\d{1,2}(\.\d+)?)x\]/gi);//find all matches of something like [1.5x] or [2x]
        if (!matches || !matches.length) {
            return null;
        }
        const lastMatch = matches[matches.length - 1];
        const numberStr = lastMatch.match(/\[(\d{1,2}(\.\d+)?)x\]/i)![1];
        var num = parseFloat(numberStr);
        return num;
    }

    private isSafariMobile() {
        const userAgent = navigator.userAgent;
        return /iP(hone|od|ad)/.test(userAgent) && /Safari/.test(userAgent) && !/Chrome/.test(userAgent);
    }
}
export var mediaService = new MediaService()

