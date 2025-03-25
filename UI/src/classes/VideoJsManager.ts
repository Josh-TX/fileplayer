import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import type videojs from "video.js";

declare global {
    interface Window {
      videojs: typeof videojs;
    }
  }
export class VideoJsManager{
    private _player!: videojs.Player;
    private _touchCount: number = 0;
    private _comboCount: number = 0;
    private _lastTouchTime: number = 0;
    private _lastTouchIndex: number = -1;
    private _lastSideTouchTime: number = 0;
    constructor(id: string){
        this.touchHandler = this.touchHandler.bind(this)
        this.clickHandler = this.clickHandler.bind(this)
        this.keydownHandler = this.keydownHandler.bind(this) 
        document.addEventListener('keydown', this.keydownHandler);
        
        this._player = window.videojs(id, {
            userActions: {
                click: true
            },
            controlBar: {
                pictureInPictureToggle: false,
                volumePanel: {inline: false}
            },
            disablePictureInPicture: true,
            playbackRates: [1, 1.5, 2, 2.5, 3]
        });
        this._player.on('userinactive', () => {
            if ((new Date().getTime() - this._lastTouchTime) < 500){
                this._player.userActive(true)
            }
        });
        this._player.ready(() => {
            var el = this._player.el().querySelector("video");
            el!.addEventListener("touchstart", this.touchHandler);
            el!.addEventListener("click", this.clickHandler);
        });
    }

    dispose(){
        document.removeEventListener('keydown', this.keydownHandler);
        if (this._player){
            this._player.dispose();
            this._player = <any>null;
        }
    }

    private keydownHandler(event: KeyboardEvent){
        if (!this._player){
            return;
        }
        if (!(<HTMLElement>event.target).closest || !(<HTMLElement>event.target).closest('.vjs-tech')){
            return;
        }
        if (event.key === ' ' || event.key === 'Spacebar') {
            if (this._player.paused()){
                this._player.play();
                this.showMiddleOverlay(false, true);
            } else {
                this._player.pause();
                this.showMiddleOverlay(true, true);
            }
        } else if (event.key === 'ArrowLeft') {
            this._player.currentTime(Math.min(this._player.currentTime() - 10, this._player.duration()));
        } else if (event.key === 'ArrowRight') {
            this._player.currentTime(Math.min(this._player.currentTime() + 10, this._player.duration()));
        }
    }

    
    private clickHandler(){
        if (this._player){
            this.showMiddleOverlay(this._player.paused(), true)
        }
    }

    private touchHandler(event: TouchEvent){
        this._touchCount++;
        if (!event.target || event.touches.length < 1){
            return;
        }
        this._lastTouchTime = new Date().getTime();
        const videoRect = (<HTMLElement>event.target).getBoundingClientRect();
        const clickX = event.touches[0].clientX - videoRect.left;
        const columnWidth = videoRect.width / 4;
        const columnIndex = Math.floor(clickX / columnWidth);
        if (this._lastTouchIndex != columnIndex){
            this._comboCount = 0;
            this._lastSideTouchTime = 0;
        }
        this._lastTouchIndex = columnIndex;
        if (columnIndex == 0 || columnIndex == 3){
            var currentColumnSideTouch = this._lastTouchTime;
            if (currentColumnSideTouch - this._lastSideTouchTime < 400){
                if (columnIndex == 0){//left
                    if (this._player.currentTime() < 1){
                        this._comboCount = 0;
                    } else {
                        this._player.currentTime(Math.min(this._player.currentTime() - 10, this._player.duration()));
                        this.showSideOverlay(true, this._comboCount*(-10));
                    }
                } else { //right
                    if (this._player.currentTime() == this._player.duration()){
                        this._comboCount = 0;
                    } else {
                        this._player.currentTime(Math.min(this._player.currentTime() + 10, this._player.duration()));
                        this.showSideOverlay(false, this._comboCount*10);
                    }
                }
                this._comboCount++;
            } else {
                this._comboCount = 1;
            }
            this._lastSideTouchTime = currentColumnSideTouch;
        } else { //middle 2 columns
            if (this._player.paused() || this._player.hasClass("vjs-user-active")){ //only toggle pause if control bar is visible
                if (this._player.paused()){
                    if (this._touchCount > 1){
                        this._player.play();
                        this.showMiddleOverlay(false, false);
                    } else {
                        setTimeout(() => {
                            this._player.play();
                            this.showMiddleOverlay(false, false);
                        });
                    }
                } else {
                    this._player.pause();
                    this.showMiddleOverlay(true, false);
                }
            }
        }
    }

    private showMiddleOverlay(isPause: boolean, full: boolean) {
        var existingEl = document.getElementById('middle-overlay');
        if (existingEl){
            existingEl.remove();
        }
        const overlay = document.createElement('div');
        overlay.id = 'middle-overlay';
        overlay.classList.add('touch-overlay');
        if (full){
            overlay.classList.add('full-middle-overlay');
        } else {
            overlay.classList.add('middle-overlay');
        }
        overlay.innerHTML = isPause ? '<span class="vjs-icon-pause"></span>': '<span class="vjs-icon-play"></span>'
        this._player.el().appendChild(overlay);
        setTimeout(() => {
            overlay.remove();
        }, 1100);
    }
    
    private showSideOverlay(isLeft: boolean, amount: number) {
        var existingEl = document.getElementById('side-overlay');
        if (existingEl){
            existingEl.remove();
        }
        const overlay = document.createElement('div');
        overlay.id = 'side-overlay';
        overlay.classList.add('touch-overlay');
        if (isLeft){
            overlay.classList.add('left-overlay');
        } else {
            overlay.classList.add('right-overlay');
        }
        overlay.innerHTML = amount > 0 ? "+" + amount : amount.toString();
        this._player.el().appendChild(overlay);
        setTimeout(() => {
            overlay.remove();
        }, 1100);
    }
}

