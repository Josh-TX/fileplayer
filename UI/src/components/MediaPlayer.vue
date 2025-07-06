<script setup lang="ts">
import { pathService } from '@/services/PathService';
import { ProgressManager } from '@/classes/ProgressManager';
import { onBeforeUnmount, onMounted, ref, shallowRef, watch, nextTick } from 'vue';
import type { MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
import BreadCrumbs from './BreadCrumbs.vue';
import { VideoJsManager } from '@/classes/VideoJsManager';
import { settingsService } from '@/services/SettingsService';
import { fileTypeHelper } from '@/services/FileTypeHelper';
import HistoryModal from "./HistoryModal.vue";
const baseUrl = import.meta.env.VITE_BASE_URL;

var src = ref<string | null>(null);
var isNative = ref<boolean | null>(null);
var isMedia = ref<boolean>(true);
var isImage = ref<boolean>(false);
var text = ref<string | null>(null);
var textLoading = ref<boolean>(false);
var showDropdown = ref<boolean>(false);
var mediaInfo = shallowRef<MediaInfo | null>(null);
var mounted = false;
var setupRan = false;
var videoJsManager: VideoJsManager | null = null;
var progressManager: ProgressManager | null = null;
var isHistoryModalOpen = ref<boolean>(false);
settingsService.getSettingsAsync().then(z => {
    isNative.value = z.useNative
    nextTick(() => {
        setup();
    })
});

watch(pathService.getPath(),(newVal, oldVal) => {
    setupRan = false;
    text.value = null;
    isImage.value = false;
    updateSrc();
    updateMediaInfo();
});
function updateSrc(){
    //we can't encodeURIComponent the path string because the slashes need to be not encoded
    var path = pathService.getPath().value.map(z => encodeURIComponent(z));
    src.value = baseUrl + "/data/" + path.join("/");
}
updateSrc();
function updateMediaInfo(){
    apiAccess.getMediaInfo(pathService.getPathString()).then(z => {
        mediaInfo.value = z
        setup();
    });
}
updateMediaInfo();
watch(isNative, (newVal, oldVal) => {
    if (oldVal != null && newVal != null){ //if oldVal was null, this was probably triggered by loading the settings
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
async function setup(){
    if (!mediaInfo.value || !mounted || isNative.value == null || setupRan){
        return;
    }
    setupRan = true;
    if (progressManager){
        progressManager.dispose();
    }
    if (videoJsManager){
        var prev = isNative.value;
        isNative.value = null;
        videoJsManager.dispose();
        await new Promise(resolve => setTimeout(resolve, 50));
        isNative.value = prev;
    }
    var pathStr = pathService.getPathString();
    if (!fileTypeHelper.isMedia(pathStr)){
        isMedia.value = false;
        if (fileTypeHelper.isText(pathStr)){
            loadText();
        } else if (fileTypeHelper.isImage(pathStr)){
            isImage.value = true;
        }
        return;
    }
    
    document.title = mediaInfo.value.fileName;
    var id: string;
    if (!isNative.value){
        id = "default-video"
        nextTick(() => {
            videoJsManager = new VideoJsManager(id);
        });
    } else {
        id = "native-video"
    }
    nextTick(() => {
        progressManager = new ProgressManager(pathStr, mediaInfo.value!.progress, id)
    });
}

async function loadText(){
    textLoading.value = true;
    text.value = await apiAccess.fetchAndLog(src.value!).then(response => response.text());
    textLoading.value = false;
}

function openInTab(){
    window.open(src.value!, '_blank');
}
function download(){
    var a = document.createElement('a');
    a.href = src.value!;
    a.download = src.value!.split('/').pop()!;
    a.click();
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
            nextTick(() => {
                isNative.value = newIsNative;
                progressManager = new ProgressManager(pathService.getPathString(), progress, "native-video");
            });
        } else {
            isNative.value = newIsNative;
            nextTick(() => {
                videoJsManager = new VideoJsManager("default-video");
                progressManager = new ProgressManager(pathService.getPathString(), progress, "default-video");
            })
        }
    }
}

function showHistory(){
    isHistoryModalOpen.value = true;
}
function hideHistory(){
    isHistoryModalOpen.value = false;
}

</script>

<template>
    <div style="display: flex">
        <div style="flex: 1 1 0; min-width: 0;">
            <BreadCrumbs></BreadCrumbs>
        </div>
        <div>
            <div class="player-toggle" @click="stopPropogate">
                <button class="btn" @click="showDropdown = !showDropdown" style="padding: 0 6px 8px; margin-bottom: -1px;">&#8230;</button>
                <div class="context-menu checkable" :class="{active: showDropdown}">
                    <div class="menu-item" @click="changePlayer(false)"><span v-if="!isNative">✓</span>VideoJS Player</div>
                    <div class="menu-item" @click="changePlayer(true)"><span v-if="isNative">✓</span>Native Player</div>
                    <div class="menu-item" @click="showHistory()">Show History</div>
                </div>
            </div>
        </div>
    </div>
    <div v-if="isMedia && isNative != null && !isNative" class="video-container">
        <video v-if="src" class="video-element video-js" id="default-video" controls>
            <source :src="src!">
            Your browser does not support the video tag.
        </video>
    </div>
    <div v-if="isMedia && isNative != null && isNative" class="video-container">
        <video v-if="src" class="video-element" id="native-video" controls>
            <source :src="src!">
            Your browser does not support the video tag.
        </video>
    </div>
    <div v-if="isImage" style="display: flex; justify-content: center;">
        <img :src="src!">
    </div>
    <div v-if="!isMedia && !text && !textLoading && !isImage" style="margin: 0 auto;">
        <h2 style="text-align: center;">file does not have a supported file extension</h2>
        <div style="display: flex; justify-content: space-between;">
            <button @click="download">Download File</button>
            <button @click="openInTab">Open in New Tab</button>
        </div>
    </div>
    <div v-if="!isMedia && (text || textLoading)" style="padding: 0 4px;">
        <pre>{{ text }}</pre>
    </div>
    <HistoryModal v-if="isHistoryModalOpen" @closed="hideHistory"></HistoryModal>
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
