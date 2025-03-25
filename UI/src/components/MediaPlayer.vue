<script setup lang="ts">
import { pathService } from '@/services/PathService';
import { ProgressManager } from '@/classes/ProgressManager';
import { onBeforeUnmount, onMounted, ref, shallowRef, watch } from 'vue';
import type { MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
import BreadCrumbs from './BreadCrumbs.vue';
import { VideoJsManager } from '@/classes/VideoJsManager';
import { settingsService } from '@/services/SettingsService';
const baseUrl = import.meta.env.VITE_BASE_URL;

var src = ref<string | null>(null);
var isNative = ref<boolean | null>(null);
var showDropdown = ref<boolean>(false);
var mediaInfo = shallowRef<MediaInfo | null>(pathService.getMediaInfo());
var mounted = false;
var videoJsManager: VideoJsManager | null = null;
var progressManager: ProgressManager | null = null;
settingsService.getSettingsAsync().then(z => {
    isNative.value = z.useNative
    setup();
});

watch(pathService.getPath(),(newVal, oldVal) => {
    updateSrc();
});
function updateSrc(){
    //we can't encodeURIComponent the path string because the slashes need to be not encoded
    var path = pathService.getPath().value.map(z => encodeURIComponent(z));
    src.value = baseUrl + "/data/" + path.join("/");
}
updateSrc();

if (!mediaInfo.value){//if mediaInfo is null, we directly loaded into the MediaPlayer rather than clicked on a media tile. 
    apiAccess.getMediaInfo(pathService.getPathString()).then(z => {
        mediaInfo.value = z
        setup();
    });
}
watch(isNative, (newVal, oldVal) => {
    if (newVal != null){
        settingsService.updateUseNative(newVal)
    }
});
onMounted(() => {
    mounted = true;
    document.body.addEventListener("click", bodyClickHandler);
    setup();
});
onBeforeUnmount(() => {
    if (videoJsManager){
        videoJsManager.dispose();
    }
    if (progressManager){
        progressManager.dispose();
    }
    document.body.removeEventListener("click", bodyClickHandler);
});
function setup(){
    if (!mediaInfo.value || !mounted || isNative.value == null){
        return;
    }
    document.title = mediaInfo.value.fileName;
    var id: string;
    if (!isNative.value){
        id = "default-video"
        setTimeout(() => {
            videoJsManager = new VideoJsManager(id);
        }, 50)
    } else {
        id = "native-video"
    }
    progressManager = new ProgressManager(pathService.getPathString(), mediaInfo.value.progress, id)
}

function stopPropogate(e: Event){
    e.stopPropagation();
}
function bodyClickHandler(){
    showDropdown.value = false;
}
function changePlayer(newIsNative: boolean){
    if (newIsNative != isNative.value){
        var progress: number | null = null;
        if (progressManager){
            progress = progressManager.dispose();
        }
        progress = progress ?? mediaInfo.value!.progress
        if (newIsNative){
            if (videoJsManager){
                videoJsManager.dispose();
                videoJsManager = null;
            }
            setTimeout(() => {
                isNative.value = newIsNative;
                progressManager = new ProgressManager(pathService.getPathString(), progress, "native-video");
            }, 50)
        } else {
            isNative.value = newIsNative;
            setTimeout(() => {
                videoJsManager = new VideoJsManager("default-video");
                progressManager = new ProgressManager(pathService.getPathString(), progress, "default-video");
            }, 50)
        }
    }
}

</script>

<template>
    <div style="display: flex">
        <div style="flex: 1 1 0;">
            <BreadCrumbs></BreadCrumbs>
        </div>
        <div>
            <div class="player-toggle" @click="stopPropogate">
                <button class="btn" @click="showDropdown = !showDropdown" style="padding: 0 6px 8px; margin-bottom: -1px;">&#8230;</button>
                <div class="context-menu checkable" :class="{active: showDropdown}">
                    <div class="menu-item" @click="changePlayer(false)"><span v-if="!isNative">✓</span>Default Player</div>
                    <div class="menu-item" @click="changePlayer(true)"><span v-if="isNative">✓</span>Native Player</div>
                </div>
            </div>
        </div>
    </div>
    <div v-if="isNative != null && !isNative" class="video-container">
        <video v-if="src" class="video-element video-js" id="default-video" controls>
            <source :src="src!">
            Your browser does not support the video tag.
        </video>
    </div>
    <div v-if="isNative != null && isNative" class="video-container">
        <video v-if="src" class="video-element" id="native-video" controls>
            <source :src="src!">
            Your browser does not support the video tag.
        </video>
    </div>
</template>

<style scoped>
    .video-container{
        min-height: 1px;
        flex: 1 0 0;
        position: relative;
        overflow: hidden;;
    }

    .video-element {
        width: 100%;
        height: 100%;
        object-fit: contain; /* Maintains aspect ratio while covering the container */
        object-position: center; /* Horizontally centers the video and aligns to the top */
    }
    .player-toggle{
        position: relative;
    }
</style>
