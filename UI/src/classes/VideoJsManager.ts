import type { MediaInfo } from "@/models/models";
import { ref, type Ref, shallowRef } from "vue";
import type videojs from "video.js";

declare global {
    interface Window {
      videojs: typeof videojs;
    }
}

type Cor = [x: number, y: number]

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
            var vid = this._player.el().querySelector("video")!;
            vid.addEventListener("touchstart", this.touchHandler);
            vid.addEventListener("click", this.clickHandler);
            vid.addEventListener("loadedmetadata", () => {
                this.initSpectrum(vid);
            });
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

    private initSpectrum(vid: HTMLVideoElement){
        if (vid.videoWidth && vid.videoHeight){
            return;//this is a video;
        }
        var canvas = document.createElement("canvas");
        canvas.classList.add("no-click")
        this._player.el().appendChild(canvas);
        const ctx = canvas.getContext("2d")!;

        canvas.width = vid.clientWidth;
        canvas.height = vid.clientHeight;
        var unit = Math.min(vid.clientWidth, vid.clientHeight) / 100;

        const audioContext = new (window.AudioContext || (<any>window).webkitAudioContext)();
        const analyser = audioContext.createAnalyser();
        analyser.fftSize = 8192; // Controls the frequency resolution (higher = more bars)
        analyser.smoothingTimeConstant = 0.7;
        const source = audioContext.createMediaElementSource(vid);
        source.connect(analyser);
        analyser.connect(audioContext.destination);

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        const scaledDataArray = new Float64Array(200);

        
        function drawTriangle(){
            requestAnimationFrame(drawTriangle);
            analyser.getByteFrequencyData(dataArray);
            copyToScaledData(dataArray, scaledDataArray, analyser.fftSize)
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            const barCount = scaledDataArray.length;
            const temp = scaledDataArray[0];
            scaledDataArray[0] = scaledDataArray[0] * 0.67 + scaledDataArray[barCount-1] * 0.33;
            scaledDataArray[barCount-1] = scaledDataArray[barCount-1] * 0.67 + temp * 0.33;
            const avgVal = scaledDataArray.reduce((sum, val) => sum + val, 0) / scaledDataArray.length;

            const centerX = canvas.width / 2;
            const centerY = canvas.height / 2;
            const focalLeftOffset = unit*7.3;
            const focalRightOffset = unit*10.6;
            const focalYOffset = unit*11.1;
            //const innerRadius = unit*5;
            //const outerRadius = unit*15
            const middleRadius = unit*5;
            const baseBarLength = unit*10;

            const PI = Math.PI;
            type Cor = [x: number, y: number]
            const A: Cor = [centerX + focalRightOffset, centerY]; //right
            const B: Cor = [centerX - focalLeftOffset, centerY + focalYOffset] //bottom left
            const C: Cor = [centerX - focalLeftOffset, centerY - focalYOffset] //top left

            // Vectors for each angle:
            const vAC1: Cor = [A[0] - C[0], A[1] - C[1]]; // Vector from C to A (for angle at C)
            const vBC1: Cor = [B[0] - C[0], B[1] - C[1]]; // Vector from C to B (for angle at C)
            const vAC2: Cor = [C[0] - A[0], C[1] - A[1]]; // Vector from A to C (for angle at A)
            const vBC2: Cor = [B[0] - A[0], B[1] - A[1]]; // Vector from A to B (for angle at A)
            const vAC3: Cor = [A[0] - B[0], A[1] - B[1]]; // Vector from B to A (for angle at B)
            const vBC3: Cor = [C[0] - B[0], C[1] - B[1]]; // Vector from B to C (for angle at B)
            // Calculate the angles
            const angleAtA = calculateAngle(vAC2, vBC2); // Angle at A
            const angleAtB = calculateAngle(vAC3, vBC3); // Angle at B
            const angleAtC = calculateAngle(vAC1, vBC1); // Angle at C
            const ABstart: Cor = polarToCor(A, middleRadius, PI / 2 - angleAtA/2);
            const ABend: Cor = polarToCor(B, middleRadius, PI/2 - angleAtA/2);
            const BCstart: Cor = polarToCor(B, middleRadius, PI);
            const BCend: Cor = polarToCor(C, middleRadius, PI);
            const CAstart: Cor = polarToCor(C, middleRadius, 3*PI / 2 + angleAtA/2);
            const CAend: Cor = polarToCor(A, middleRadius, 3*PI / 2 + angleAtA/2);

            const Acircum = 2*PI*middleRadius/(PI-angleAtA);
            const Bcircum = 2*PI*middleRadius/(PI-angleAtB);
            const Ccircum = 2*PI*middleRadius/(PI-angleAtC);
            const ABdist = distance(ABstart, ABend);
            const BCdist = distance(BCstart, BCend);
            const CAdist = distance(CAstart, CAend);

            //draw dots to assist in context
            // ctx.fillStyle = "white";
            // ctx.beginPath();
            // ctx.arc(centerX, centerY, 3, 0, PI * 2);
            // ctx.fill();

            // ctx.fillStyle = "red";
            // ctx.beginPath();
            // ctx.arc(...A, 3, 0, PI * 2);
            // ctx.fill();

            // ctx.beginPath();
            // ctx.arc(...B, 3, 0, PI * 2);
            // ctx.fill();
            // ctx.beginPath();
            // ctx.arc(...C, 3, 0, PI * 2);
            // ctx.fill();
            

            //ctx.beginPath();

            ////draw solid logo
            //ctx.arc(...A, innerRadius, 0, PI / 2 - angleAtA/2);
            //ctx.arc(...B, innerRadius, PI/2 - angleAtA/2, PI);
            //ctx.arc(...C, innerRadius, PI, 3*PI/2 + angleAtA/2);
            //ctx.arc(...A, innerRadius, 3*PI / 2 + angleAtA/2, 0);
            //ctx.arc(...A, outerRadius, 0, 3*PI / 2 + angleAtA/2, true);
            //ctx.arc(...C, outerRadius, 3*PI/2 + angleAtA/2, PI, true);
            //ctx.arc(...B, outerRadius, PI, PI/2 - angleAtA/2, true);
            //ctx.arc(...A, outerRadius, PI / 2 - angleAtA/2, 0, true);
            //ctx.closePath();
            //ctx.fill();
            var segments = [
                Acircum / 2, ABdist, Bcircum, BCdist, Ccircum, CAdist, Acircum / 2
            ]
            var pathLength = segments.reduce((a,b) => a+b, 0);//sum
            var segmentAccPercents = Array(segments.length).fill(0);
            for (var i = 0; i < segments.length; i++){
                segmentAccPercents[i] = segments[i] / pathLength + (i > 0 ? segmentAccPercents[i-1] : 0);
            }
            const barGradient = ctx.createLinearGradient(centerX - 20, centerY+20, centerX + 30, centerY-30);
            barGradient.addColorStop(0, "rgba(164, 107, 255, 1)");
            barGradient.addColorStop(0.4, "rgba(164, 107, 255, 1)");
            barGradient.addColorStop(0.6, "rgba(71, 162, 248, 1)");
            barGradient.addColorStop(1, "rgba(71, 162, 248, 1)");
            const lowerBound = baseBarLength * (1 - Math.min(Math.max(0, avgVal - 20), 100) / 100 * 0.5);
            const upperBound = baseBarLength + baseBarLength * (1 + Math.min(avgVal, 100) / 100 * 0.5);
            for (var i = 0; i < barCount; i++){
                const freqValue = scaledDataArray[i] / 255; 
                const barLength = lerp(lowerBound, upperBound, freqValue);
                var percent = i / barCount;
                var segmentIndex = segmentAccPercents.findIndex(z => z > percent);
                var segmentStartPercent = segmentIndex == 0 ? 0 : segmentAccPercents[segmentIndex - 1];
                var segmentEndPercent = segmentIndex == segments.length - 1 ? 1 : segmentAccPercents[segmentIndex];
                var p = (percent - segmentStartPercent) / (segmentEndPercent - segmentStartPercent);
                let startCor: Cor | undefined;
                let angle: number | undefined;
                switch(segmentIndex){
                    case 0:
                        angle = lerp(0, PI / 2 - angleAtA/2, p)
                        startCor = polarToCor(A, middleRadius, angle);
                        break;
                    case 1:
                        angle = PI / 2 - angleAtA/2;
                        startCor = [lerp(ABstart[0], ABend[0], p), lerp(ABstart[1], ABend[1], p)];
                        break;
                    case 2:
                        angle = lerp(PI/2 - angleAtA/2, PI, p)
                        startCor = polarToCor(B, middleRadius, angle);
                        break;
                    case 3:
                        angle = PI;
                        startCor = [lerp(BCstart[0], BCend[0], p), lerp(BCstart[1], BCend[1], p)];
                        break;
                    case 4:
                        angle = lerp(PI, 3*PI/2 + angleAtA/2, p)
                        startCor = polarToCor(C, middleRadius, angle);
                        break;
                    case 5:
                        angle = 3*PI/2 + angleAtA/2;
                        startCor = [lerp(CAstart[0], CAend[0], p), lerp(CAstart[1], CAend[1], p)];
                        break;
                    case 6:
                        angle = lerp(3*PI/2 + angleAtA/2, 2*PI, p)
                        startCor = polarToCor(A, middleRadius, angle);
                        break;
                }
                if (startCor != null && angle != null){
                    var endCor = polarToCor(startCor, barLength, angle)
                    ctx.strokeStyle = barGradient
                    ctx.lineWidth = 2
                    ctx.beginPath();
                    ctx.moveTo(...startCor);
                    ctx.lineTo(...endCor);
                    ctx.stroke();
                }
            }

        }
        vid.addEventListener("play", () => {
            if (audioContext.state === "suspended") {
                audioContext.resume();
            }
        });
        drawTriangle();
    }
}

function copyToScaledData(fftDataArray: Uint8Array, scaledDataArray: Float64Array, windowSize: number){
    var minFrequencyHz = 116.54 //58.27 //bflat1
    var maxFrequencyHz = 3729.31 //bflat7
    var minFrequencyHz = 55
    var maxFrequencyHz = 3450
    var sampleRate = 44100
    for (var i = 0; i < scaledDataArray.length; i++){
        var percent = inverseLerp(0, scaledDataArray.length - 1, i);
        var hz = expInterp(minFrequencyHz, maxFrequencyHz, percent)
        var fftIndex = (hz * windowSize) / sampleRate;
        scaledDataArray[i] = adjecentAvg(fftDataArray, fftIndex);
    }
}


function adjecentAvg(arr: Float32Array | Uint8Array, index: number): number {
    const lower = Math.floor(index);
    const upper = Math.ceil(index);
    const weight = index - lower;
    return (arr[lower] ?? 0) * (1 - weight) + (arr[upper] ?? 0) * weight;
}


function lerp(startVal: number, endVal: number, percent: number) {
    return startVal + (endVal - startVal) * percent;
}

//returns a percentage corresponding to mid's progress from start to end
function inverseLerp(start: number, end: number, mid: number): number {
    return (start - mid) / (start - end);
}

function expInterp(a: number, b: number, t: number): number {
    return a * Math.pow(b / a, t);
}

function dotProduct(v1: Cor, v2: Cor): number {
    return v1[0] * v2[0] + v1[1] * v2[1];
}

// Function to calculate the magnitude of a vector
function magnitude(v: Cor): number {
    return Math.sqrt(v[0] ** 2 + v[1] ** 2);
}

function polarToCor(center: Cor, r: number, angle: number): Cor {
    return [center[0] + r * Math.cos(angle), center[1] + r * Math.sin(angle)]
}

function distance(c1: Cor, c2: Cor): number {
    const dx = c2[0] - c1[0];
    const dy = c2[1] - c1[1];
    return Math.sqrt(dx * dx + dy * dy);
}

// Function to calculate the angle in degrees between two vectors
function calculateAngle(v1: Cor, v2: Cor): number {
    const dotProd = dotProduct(v1, v2);
    const magV1 = magnitude(v1);
    const magV2 = magnitude(v2);
    const cosTheta = dotProd / (magV1 * magV2);
    const angleRad = Math.acos(cosTheta); // angle in radians
    //const angleDeg = angleRad * (180 / Math.PI); // convert to degrees
    return angleRad;
}
