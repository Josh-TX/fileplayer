<script setup lang="ts">
import type { FolderInfo, MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
import { pathService } from '@/services/PathService';
import { computed, onMounted, onUnmounted, ref } from 'vue';
var emits = defineEmits(['changed'])
const props = defineProps<{
    folderInfo?: FolderInfo | undefined,
    mediaInfo?: MediaInfo | undefined
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
    var secondsCount = props.mediaInfo!.duration;
    if (secondsCount == null){
        return "\xa0\xa0"
    }
    const hours = Math.floor(secondsCount / 3600);
    const minutes = Math.floor((secondsCount % 3600) / 60);
    const seconds = secondsCount % 60;

    return [
        hours > 0 ? String(hours) : null, // Include hours only if greater than 0
        String(minutes).padStart(hours > 0 ? 2 : 1, '0'), // Ensure minutes are padded if hours are included
        String(seconds).padStart(2, '0') // Always pad seconds
    ].filter(Boolean).join(':'); // Filter out `null` and join with colons
});

var showDropdown = ref<boolean>(false);
function dotsClicked(event: Event){
    event.stopPropagation();
    showDropdown.value = !showDropdown.value;
}

function bodyClickHandler(){
    showDropdown.value = false;
}
onMounted(() => {
    document.body.addEventListener("click", bodyClickHandler);
})
onUnmounted(() => {
    document.body.removeEventListener("click", bodyClickHandler);
})

async function rename(){
    var oldName = computedName.value;
    var newName = prompt("enter a new name", oldName);
    if (newName && newName != oldName){
        await apiAccess.rename([...pathService.getPath().value, oldName].join("/"), newName);
        emits("changed");
    }
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
        await apiAccess.delete([...pathService.getPath().value, name].join("/"));
        emits("changed");
    }
}

</script>

<template>

    <div class="tile-container">
        <div class="left-side">
            <template v-if="mediaInfo">
                <div class="ext">{{ computedExtension }}</div>
                <img src="/file-icon.png">
            </template>
            <img v-else src="/folder-icon.png">
        </div>
        <div class="right-side">
            <h2 class="primary-name">{{ computedName }}</h2>
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
        <div class="dots" @click="dotsClicked">
            &#8230;
            <div class="context-menu" :class="{active: showDropdown}">
                <div class="menu-item" @click="rename">Rename</div>
                <div class="menu-item" @click="del">Delete</div>
            </div>
        </div>
    </div>
</template>

<style scoped>

.tile-container {
    cursor: pointer;
    height: 48px;
    display: flex;
    background: #222;
    box-shadow: 2px 2px 3px rgba(0,0,0,0.2);
    position: relative;
}

.dots{
    position: absolute;
    right: 0;
    top: 0;
    padding: 2px 6px 8px 6px;;
    font-weight: 700;
    font-size: 14px;
    color: #ccc;
    border-radius: 2px;
}

.dots:hover {
    background-color: rgba(0,0,0,0.2);
    color: white;
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
    background-color: rgb(0, 183, 255);
    color: white;
    box-shadow: 1px 1px 2px rgba(0,0,0,0.2);
}

.info-container{
    position: relative;
    display: flex;
    font-size: 13px;
    justify-content: space-between;
    margin-top: 1px;
    margin-right: 4px;
    color: #ccc;
}

.modify-date{
    position: absolute;
    top: 0;
    left: 35%;
}
</style>