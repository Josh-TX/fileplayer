<script setup lang="ts">
import type { FolderInfo, MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
import { fileTypeHelper } from '@/services/FileTypeHelper';
import { formatDuration } from '@/services/MiscHelpers';
import { pathService } from '@/services/PathService';
import { modalService } from '@/services/ModalService';
import { computed, onMounted, onUnmounted, ref } from 'vue';
var emits = defineEmits(['changed'])
const props = defineProps<{
    folderInfo?: FolderInfo | undefined,
    mediaInfo?: MediaInfo | undefined,
    allowChanges?: boolean | undefined,
    url?: string | undefined,
}>()

const computedName = computed(() => {
    return props.folderInfo ? props.folderInfo.folderName : props.mediaInfo!.fileName;
});

const computedExtension = computed(() => {
    return props.mediaInfo!.fileName.split('.').pop();
});

const computedDate = computed(() => {
    var date = props.folderInfo ? new Date(props.folderInfo.modifyDate) : new Date(props.mediaInfo!.modifyDate);
    return new Intl.DateTimeFormat('en-US').format(date);
});

function getClass(){
    if (props.folderInfo){
        return "folder"
    }
    if (fileTypeHelper.isVideo(props.mediaInfo!.fileName)){
        return "video";
    }
    if (fileTypeHelper.isAudio(props.mediaInfo!.fileName)){
        return "audio";
    }
    return "nonmedia"
};

function isMedia(){
    return props.mediaInfo && fileTypeHelper.isMedia(props.mediaInfo.fileName);
};

function isDownload(){
    return props.folderInfo && /^temp-download-folder-\d\d-\d\d-\d\d$/.test(props.folderInfo.folderName);
};

const computedSize = computed(() => {
    var bytes = props.folderInfo ? props.folderInfo.mediaDiskSize : props.mediaInfo!.fileSize;
    if (bytes === 0) return '0 B';
    const units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];
    const index = Math.floor(Math.log(bytes) / Math.log(1024));
    const size = bytes / Math.pow(1024, index);

    // Format to 3 significant digits
    const significantSize = size < 10 
        ? size.toFixed(2) 
        : size < 100 
        ? size.toFixed(1) 
        : Math.round(size);

    return `${significantSize} ${units[index]}`;
});

const computedDuration = computed(() => {
    return formatDuration(props.mediaInfo!.duration)
});

var showDropdown = ref<boolean>(false);
function dotsClicked(event: Event){
    event.stopPropagation();
    event.preventDefault();
    showDropdown.value = !showDropdown.value;
}
function stopEvent(event: Event){
    event.stopPropagation();
}
function bodyClickHandler(){
    showDropdown.value = false;
}
onMounted(() => {
    document.body.addEventListener("mousedown", bodyClickHandler);
})
onUnmounted(() => {
    document.body.removeEventListener("mousedown", bodyClickHandler);
})

async function rename(){
    var oldName = computedName.value;
    var newName = prompt("enter a new name", oldName);
    if (newName && newName != oldName){
        await apiAccess.rename([...pathService.getPath().value, oldName].join("/"), newName);
        emits("changed");
    }
}

async function copy(isMove: boolean){
    var folderCount = props.folderInfo != null ? 1 : 0;
    modalService.startMove(pathService.getPath().value, [[...pathService.getPath().value, computedName.value].join("/")], folderCount, isMove);
}

function download(){
  var a = document.createElement('a');
  var path = [...pathService.getPath().value, props.mediaInfo!.fileName].map(z => encodeURIComponent(z));
  var url = [window.location.origin, "data", ...path].join("/")
  a.href = url;
  a.download = url.split('/').pop()!;
  a.click();
}


async function del(){
    var name = computedName.value;
    var msg = `delete file ${name}?`
    if (props.folderInfo != null){
        if (props.folderInfo.mediaFileCount > 0){
            msg = `delete folder ${name} and all its contents? it contains ${props.folderInfo.mediaFileCount} media files (${computedSize.value})`;
        } else {
            msg = `delete folder ${name}?`;
        }
    }
    if (confirm(msg)){
        await apiAccess.deleteItems({
            filePaths: [[...pathService.getPath().value, name].join("/")]
        });
        emits("changed");
    }
}

</script>

<template>

    <component :is="url ? 'a' : 'div'" :href="url ? url : null" class="tile-container" :class="getClass()">
        <div class="left-side">
            <template v-if="mediaInfo">
                <div class="ext" v-if="isMedia()">{{ computedExtension }}</div>
                <img src="/file-icon.png">
            </template>
            <template v-else>
                <img v-if="isDownload()" src="/folder-download-icon.png">
                <img v-else src="/folder-icon.png">
            </template>
        </div>
        <div class="right-side">
            <h2 class="primary-name" :title="computedName">{{ computedName }}</h2>
            <div class="info-container">
                <template v-if="mediaInfo">
                    <div>{{ computedSize }}</div>
                    <div class="modify-date">{{ computedDate }}</div>
                    <div>{{ computedDuration }}</div>
                </template>
                <template v-if="folderInfo">
                    <div>{{ computedSize }}</div>
                    <div class="modify-date">{{ computedDate }}</div>
                    <div>{{ folderInfo.mediaFileCount }}</div>
                </template>
            </div>
        </div>
        <div v-if="props.allowChanges" class="dots" @click="dotsClicked">
            &#8230;
            <div class="context-menu" :class="{active: showDropdown}" @mousedown="stopEvent"> 
                <div class="menu-item" @click="rename">Rename</div>
                <div class="menu-item" @click="copy(true)">Move</div>
                <div class="menu-item" @click="copy(false)">Copy</div>
                <div v-if="mediaInfo" class="menu-item" @click="download()">Download</div>
                <div class="menu-item" @click="del">Delete</div>
            </div>
        </div>
        <div v-if="mediaInfo && mediaInfo.progress != null" class="progress" :style="{width: (mediaInfo.progress*100)+'%'}"></div>
        <div v-if="mediaInfo && mediaInfo.progress != null" class="progress progress-gray"></div>
    </component>
</template>

<style scoped>

.video {
    --media-color: #a46bff;
}
.audio {
    --media-color: #47a2f8;
}

.tile-container {
    cursor: pointer;
    height: 48px;
    display: flex;
    background: var(--tile-bg);
    box-shadow: var(--tile-shadow);
    position: relative;
    touch-action: manipulation;
    -webkit-tap-highlight-color: transparent;
}

.dots{
    position: absolute;
    right: 0;
    top: 0;
    padding: 2px 6px 8px 6px;;
    font-weight: 700;
    font-size: 14px;
    color: var(--dots-color);
    border-radius: 2px;
}

.dots:hover {
    background-color: rgba(0,0,0,0.2);
    color: var(--dots-active-color);
}

.left-side {
    position: relative;
    width: 36px;
    padding: 6px;
    text-align: right;
}

.left-side>img {
    height: 36px;
}

.right-side{
    flex: 1 0 0;
    overflow: hidden;;
}

.primary-name {
    overflow: hidden;
    font-weight: 400;
    width: 100%;
    min-width: 100%;
    margin: 1px 0 0 0;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.ext {
    position: absolute;
    top: 18px;
    padding: 2px;
    font-size: 14px;
    text-transform: uppercase;
    border-radius: 3px;
    background-color: var(--media-color);
    color: white;
    box-shadow: 1px 1px 2px rgba(0,0,0,0.2);
    max-width: 36px;
    overflow: hidden;
}

.info-container{
    position: relative;
    display: flex;
    font-size: 13px;
    justify-content: space-between;
    margin-top: 1px;
    margin-right: 4px;
    color: var(--text-muted);
}

.modify-date{
    position: absolute;
    top: 0;
    left: 35%;
}

.progress{
    position: absolute;
    z-index: 2;
    left: 0;
    bottom: 0;
    height: 3px;
    background-color: var(--media-color);
}

.progress-gray{
    background-color: var(--progress-gray);
    z-index: 1;
    width: 100%;
}

</style>