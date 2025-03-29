import { apiAccess } from '@/services/ApiAccess';
import { settingsService } from '@/services/SettingsService';


export class ProgressManager {
    private _id: string;
    private _intervalId: number | null;
    private _initFails: number = 0;
    private _initTime: number
    private _path: string;
    private _lastProgress: number | null = null;
    private _lastUpdateTime: number | null = null;
    private _lastPlaybackRate: number = 1;
    private _staticPlaybackRate: number | null = null;
    constructor(path: string, progress: number | null, id: string){
        this.intervalCallback = this.intervalCallback.bind(this);
        this._id = id;
        this._path = path;
        this._intervalId = setInterval(this.intervalCallback, 200);
        this._initTime = new Date().getTime();
        this.tryInit(progress);
    }

    private tryInit(progress: number | null){
        var el = this.getEl();
        
        if (!el){
            if (this._initFails >= 10){
                throw "progress manager failed to get video element after 10 attempts";
            }
            setTimeout(() => {
                this.tryInit(progress);
            }, 10);
            return;
        }
        this._staticPlaybackRate = this.extractStaticSpeed(this._path);
        if (this._staticPlaybackRate != null){
            this.runManyTimes(10, () => el!.playbackRate = this._staticPlaybackRate!); //default play
        } else {
            settingsService.getSettingsAsync().then(z => {
                this._lastPlaybackRate = z.playbackSpeed;
                this.runManyTimes(10, () => {
                    el = this.getEl();
                    if (el){
                        el.playbackRate = z.playbackSpeed
                    }
                });
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
                this._lastProgress = (el.currentTime / el.duration)
                this.runManyTimes(10, () => {
                    el = this.getEl();
                    if (el && !isNaN(el.duration)){
                        el.currentTime = el.duration * progress
                    }
                });
            } else {
                el.addEventListener('loadedmetadata', () => {
                    if (!this.isSafariMobile() && el){
                        this._lastProgress = (el.currentTime / el.duration)
                        this.runManyTimes(10, () => {
                            el = this.getEl();
                            if (el && !isNaN(el.duration)){
                                el.currentTime = el.duration * progress
                            }
                        });
                    }
                });
            }
        }
    }

    dispose(): number | null{
        var el = this.getEl();
        if (this._intervalId){
            clearInterval(this._intervalId);
        }
        return el ? (el.currentTime / el.duration) : this._lastProgress;
    }

    private intervalCallback(){
        var el = this.getEl();
        if (!el){
            return;
        }
        if (typeof el.currentTime != "number"){
            el = <HTMLVideoElement>el.querySelector("video");
        }
        if (!el){
            return;
        }
        var timeSinceInit =  new Date().getTime() - this._initTime;
        if (el.playbackRate != this._lastPlaybackRate && this._staticPlaybackRate == null && timeSinceInit > 3000){
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
    }

    private runManyTimes(times: number, callback: () => void){
        if (times >= 0){
            callback();
            setTimeout(() => this.runManyTimes(times - 1, callback));
        }
    }

    private getEl(): HTMLVideoElement | null{
        var el = <HTMLVideoElement>document.getElementById(this._id);
        if (!el){
            return null;
        }
        if (typeof el.currentTime == "number"){
            return <HTMLVideoElement>el;
        }
        el = <HTMLVideoElement>el.querySelector("video");
        return el ? el : null;
    }

    private updateProgress(el: HTMLVideoElement){
        this._lastProgress = (el.currentTime / el.duration);
        this._lastUpdateTime = new Date().getTime();
        if (!isNaN(this._lastProgress)){
            apiAccess.updateProgress(this._lastProgress , this._path!);
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

