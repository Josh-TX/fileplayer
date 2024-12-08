<script setup lang="ts">

import { watch, shallowRef, type ShallowRef, ref, onUnmounted, onMounted } from 'vue';
import { apiAccess } from '@/services/ApiAccess';
import FolderItem from './FolderItem.vue';
import { pathService } from '@/services/PathService';
import type { FolderInfo, MediaInfo } from '@/models/models';
import MediaItem from './MediaItem.vue';
import { settingsService } from '@/services/SettingsService';

function handleMediaClick(mediaInfo: MediaInfo) {
    pathService.appendFile(mediaInfo);
}

// Emit "folder-clicked" event with a string payload
function handleFolderClick(folderInfo: FolderInfo) {
    pathService.appendFolder(folderInfo.folderName);
}

var folderInfos = shallowRef<FolderInfo[]>([]);
var mediaInfos = shallowRef<MediaInfo[]>([]);
var showDropdown = ref<boolean>(false);
var sortDesc = ref<boolean>(true);
var sortBy = ref<string>("name");
var settingsPromise = settingsService.getSettingsAsync();
watch(pathService.getWatcherPathRef(), (newVal, oldVal) => {
    load();
});

async function load() {
    if (!pathService.isFile().value) {
        var result = await apiAccess.getDirContents(pathService.getPathString());
        var settings = await settingsPromise;
        console.log('settings', settings)
        sortBy.value = settings.sortBy;
        sortDesc.value = settings.sortDesc;
        folderInfos.value = result.folderInfos
        mediaInfos.value = result.mediaInfos
        sort();
        if (result.mediaInfos.some(z => z.duration == null)) {
            var durationsResponse = await apiAccess.getDurations(pathService.getPathString());
            if (mediaInfos.value != result.mediaInfos){
                //path must have changed
                return;
            }
            var updatedMediaInfos = result.mediaInfos.map(mediaInfo => {
                var foundDuration = durationsResponse.mediaDurations.find(z => z.fileName == mediaInfo.fileName);
                if (foundDuration != null){
                    mediaInfo.duration = foundDuration.duration;
                }
                return {...mediaInfo}
            });
            mediaInfos.value = updatedMediaInfos
        }
    }
}
load();

function changeSortBy(newSortBy: string){
    sortBy.value = newSortBy;
    setTimeout(() => {
        showDropdown.value = false;
    }, 150)
    settingsService.updateSort(sortBy.value, sortDesc.value);
    sort();
}
function toggleSortDesc(){
    sortDesc.value = !sortDesc.value
    settingsService.updateSort(sortBy.value, sortDesc.value);
    folderInfos.value.reverse();
    mediaInfos.value.reverse();
}

function stopPropogate(e: Event){
    e.stopPropagation();
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

function sort(){
    var modifier = sortDesc.value ? 1 : -1;
    if (sortBy.value == "modified"){
        folderInfos.value.sort((z1, z2) => new Date(z2.modifyDate).getTime() - new Date(z1.modifyDate).getTime())
        mediaInfos.value.sort((z1, z2) => new Date(z2.modifyDate).getTime() - new Date(z1.modifyDate).getTime())
    }
    else if (sortBy.value == "duration"){
        mediaInfos.value.sort((z1, z2) => (z2.duration || 0) - (z1.duration || 0))
    }    
    else if (sortBy.value == "size"){
        folderInfos.value.sort((z1, z2) => z2.mediaDiskSize - z1.mediaDiskSize)
        mediaInfos.value.sort((z1, z2) => z2.fileSize - z1.fileSize)
        
    }
    else if (sortBy.value == "played"){
        mediaInfos.value.sort((z1, z2) => {
            if (z1.progressDate == null && z2.progressDate != null){
                return 1;
            } 
            if (z1.progressDate != null && z2.progressDate == null){
                return -1;
            }
            if (z1.progressDate != null && z2.progressDate != null){
                return new Date(z2.progressDate).getTime() - new Date(z1.progressDate).getTime()
            }
            return 0;
        })
    } 
    else {
        folderInfos.value.sort((z1, z2) => z1.folderName.localeCompare(z2.folderName))
        mediaInfos.value.sort((z1, z2) => z1.fileName.localeCompare(z2.fileName))
    }
    if (!sortDesc.value){
        folderInfos.value.reverse();
        mediaInfos.value.reverse();
    }
    folderInfos.value = [...folderInfos.value]
    mediaInfos.value = [...mediaInfos.value]
}

</script>

<template>
    <div style="position: relative;">
        <div class="sort-btn-container" @click="stopPropogate">
            <button class="btn" @click="showDropdown = !showDropdown">sort by</button>
            <button class="btn" style="font-family: monospace" @click="toggleSortDesc">{{ sortDesc ? "↓" : "↑" }}</button>
            <div class="context-menu" :class="{active: showDropdown}">
                <div class="menu-item" @click="changeSortBy('name')"><span v-if="sortBy=='name'">✓</span>name</div>
                <div class="menu-item" @click="changeSortBy('modified')"><span v-if="sortBy=='modified'">✓</span>date modified</div>
                <div class="menu-item" @click="changeSortBy('duration')"><span v-if="sortBy=='duration'">✓</span>duration</div>
                <div class="menu-item" @click="changeSortBy('size')"><span v-if="sortBy=='size'">✓</span>size</div>
                <div class="menu-item" @click="changeSortBy('played')"><span v-if="sortBy=='played'">✓</span>date played</div>
            </div>
        </div>
        <div class="dir-content-grid">
            <div v-for="folderInfo of folderInfos">
                <FolderItem :folderInfo="folderInfo" @click="handleFolderClick(folderInfo)"></FolderItem>
            </div>
            <div v-for="mediaInfo of mediaInfos">
                <MediaItem :mediaInfo="mediaInfo" @click="handleMediaClick(mediaInfo)"></MediaItem>
            </div>
        </div>
    </div>
</template>

<style>
.sort-btn-container{
    position: absolute;
    bottom: 100%;
    right: 8px;
}
.context-menu {
    
    padding: 4px 0;
    display: none;
    border-radius: 4px;
    z-index: 5;
    flex-direction: column;
    position: absolute;
    top: 100%;
    right: 0;
    --bg-main: #333;
    --border-color-contrast: #666;
    background: var(--bg-main);
    border: 1px solid var(--border-color-contrast);
    box-shadow: 0 2px 4px -1px #0003,
    0 4px 5px #00000024,
    0 1px 10px #0000001f;
}
.context-menu.active {
    display: flex;
}

.menu-item {
    color: var(--text-main);
    padding: 6px 12px 6px 24px;
    font-size: 14px;
    cursor: pointer;
    position: relative;
    white-space: nowrap;
}
.menu-item>span{
    position: absolute;
    top: 4px;
    left: 8px;
}
.menu-item:hover {
    --text-contrast: #FFF;
    --bg-hover: #444;
    color: var(--text-contrast);
    background: var(--bg-hover);
}

.btn{
    cursor: pointer;
    outline: none;
    border: none;
    color: white;
    background: none;
    padding: 4px 8px;
    border-radius: 3px;;
    margin-bottom: 2px;
}
.btn:hover{
    background: rgba(0,0,0,0.2);
}
.sort-dropdown{
    padding: 4px;
    display: none;
    position: absolute;
    top: 100%;
    right: 0;
    border: 1px solid #555;
    background-color: #333;;
}
.sort-dropdown.show{
    display: block;
}

.dir-content-grid{
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 10px;
    max-width: 800px;
    margin: 0 auto;
    padding: 0 10px 10px 10px
}

@media (min-width: 1200px) {
    .dir-content-grid{
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        max-width: 1600px;
    }
}

</style>