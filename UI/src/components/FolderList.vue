<script setup lang="ts">

import { watch, shallowRef, type ShallowRef, ref, onUnmounted, onMounted } from 'vue';
import { apiAccess } from '@/services/ApiAccess';
import { pathService } from '@/services/PathService';
import { modalService } from '@/services/ModalService';
import type { FolderInfo, MediaInfo } from '@/models/models';
import { settingsService } from '@/services/SettingsService';
import UploadFileModal from './UploadFileModal.vue';
import UploadFromUrl from './UploadUrlModal.vue'
import Tile from './Tile.vue';

function handleMediaClick(mediaInfo: MediaInfo) {
    if (!isBulkEdit.value){
        pathService.appendFile(mediaInfo);
    } else {
        if (selectedMediaInfos.value.includes(mediaInfo)){
            selectedMediaInfos.value = selectedMediaInfos.value.filter(z => z != mediaInfo);
        } else {
            selectedMediaInfos.value = [...selectedMediaInfos.value, mediaInfo]
        }
    }
}

function handleFolderClick(folderInfo: FolderInfo) {
    if (!isBulkEdit.value){
        pathService.appendFolder(folderInfo.folderName);
    } else {
        if (selectedFolderInfos.value.includes(folderInfo)){
            selectedFolderInfos.value = selectedFolderInfos.value.filter(z => z != folderInfo);
        } else {
            selectedFolderInfos.value = [...selectedFolderInfos.value, folderInfo]
        }
    }
}

var folderInfos = shallowRef<FolderInfo[]>([]);
var mediaInfos = shallowRef<MediaInfo[]>([]);

var filteredFolderInfos = shallowRef<FolderInfo[]>([]);
var filteredMediaInfos = shallowRef<MediaInfo[]>([]);

var selectedFolderInfos = shallowRef<FolderInfo[]>([]);
var selectedMediaInfos = shallowRef<MediaInfo[]>([]);

var showDropdown = ref<boolean>(false);
var sortDesc = ref<boolean>(true);
var isBulkEdit = ref<boolean>(false);
var isFilter = ref<boolean>(false);
var filterText = ref<string>("");
var sortBy = ref<string>("name");
var filterChangedTimeout: number | undefined;

watch(pathService.getPath(), (newVal, oldVal) => {
    load();
});
modalService.registerOnRefresh(refresh);

async function load() {
    if (!pathService.isFile().value) {
        var pathString = pathService.getPathString();
        var result = await apiAccess.getDirContents(pathString);
        var settings = await settingsService.getSettingsAsync();
        sortBy.value = settings.sortBy;
        sortDesc.value = settings.sortDesc;
        folderInfos.value = result.folderInfos
        mediaInfos.value = result.mediaInfos
        sort();
        updateFilter();
        if (result.mediaInfos.some(z => z.duration == null)) {
            var durationsResponse = await apiAccess.getDurations(pathString);
            if (pathString != pathService.getPathString()){
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
            updateFilter();
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
    updateFilter();
}

async function newFolder(){
    var newName = prompt("enter new folder's name");
    if (newName){
        await apiAccess.createDir([...pathService.getPath().value, newName].join("/"));
        refresh();
    }
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

function startBulkEdit(){
    isBulkEdit.value = true;
}

function stopBulkEdit(){
    selectedFolderInfos.value = [];
    selectedMediaInfos.value = [];
    isBulkEdit.value = false;
}

function toggleSelectAll(){
    if ((selectedFolderInfos.value.length + selectedMediaInfos.value.length) == (filteredMediaInfos.value.length + filteredFolderInfos.value.length)){
        selectedFolderInfos.value = [];
        selectedMediaInfos.value = [];
    } else {
        selectedFolderInfos.value = [...filteredFolderInfos.value]
        selectedMediaInfos.value = [...filteredMediaInfos.value];
    }
}

function bulkCopy(isMove: boolean){
    var path = pathService.getPath().value;
    var folderPaths = selectedFolderInfos.value.map(z => [...path, z.folderName].join("/"));
    var mediaPaths = selectedMediaInfos.value.map(z => [...path, z.fileName].join("/"));
    modalService.startMove(path, folderPaths.concat(mediaPaths), folderPaths.length, isMove);
    stopBulkEdit();
}

async function bulkDelete(){
    var path = pathService.getPath().value;
    var folderPaths = selectedFolderInfos.value.map(z => [...path, z.folderName].join("/"));
    var mediaPaths = selectedMediaInfos.value.map(z => [...path, z.fileName].join("/"));
    var msg = mediaPaths.length == 1 ? "delete 1 file" : "delete " + mediaPaths.length + " files";
    if (folderPaths.length > 0){
        var msg = mediaPaths.length == 0 ? "delete " : msg + " and ";
        msg += folderPaths.length == 1 ? "1 folder?" : folderPaths.length + " folders?";
    } else {
        msg += "?"
    }
    if (confirm(msg)){
        await apiAccess.deleteItems({
            filePaths: folderPaths.concat(mediaPaths)
        });
        refresh();
    }
}

function toggleIsFilter(){
    isFilter.value = !isFilter.value;
    setTimeout(() => {
        var el = <HTMLInputElement>document.getElementById("filter-input");
        if (el){
            el.focus();
            el.select();
        }
    });
    updateFilter();
}

function filterChanged(){
    clearTimeout(filterChangedTimeout);
    filterChangedTimeout = setTimeout(() => {
        updateFilter();
    }, 150)
}

function updateFilter(){
    if (isFilter.value){
        var lower = filterText.value.toLowerCase();
        filteredFolderInfos.value = folderInfos.value.filter(z => z.folderName.toLowerCase().includes(lower));
        filteredMediaInfos.value = mediaInfos.value.filter(z => z.fileName.toLowerCase().includes(lower));
        selectedFolderInfos.value = selectedFolderInfos.value.filter(z => z.folderName.toLowerCase().includes(lower));
        selectedMediaInfos.value = selectedMediaInfos.value.filter(z => z.fileName.toLowerCase().includes(lower));
    } else {
        filteredFolderInfos.value = folderInfos.value
        filteredMediaInfos.value = mediaInfos.value
    }
}

function refresh(){
    load();
}

</script>

<template>
    <div style="position: relative;">
        <div class="sort-btn-container" @click="stopPropogate">
            <button class="btn" @click="toggleIsFilter" style="margin-right: 4px;">filter</button>
            <button class="btn" @click="showDropdown = !showDropdown">
                sort by
                <div class="context-menu checkable" :class="{active: showDropdown}">
                    <div class="menu-item" @click="changeSortBy('name')"><span v-if="sortBy=='name'">✓</span>name</div>
                    <div class="menu-item" @click="changeSortBy('modified')"><span v-if="sortBy=='modified'">✓</span>date modified</div>
                    <div class="menu-item" @click="changeSortBy('duration')"><span v-if="sortBy=='duration'">✓</span>duration</div>
                    <div class="menu-item" @click="changeSortBy('size')"><span v-if="sortBy=='size'">✓</span>size</div>
                </div>
            </button>
            <button class="btn" style="font-family: monospace" @click="toggleSortDesc">{{ sortDesc ? "↓" : "↑" }}</button>
        </div>
        <div v-if="isFilter" style="display: flex; justify-content: end; padding: 0 8px 4px 0;">
            <input id="filter-input" v-model="filterText" @input="filterChanged()">
        </div>
        <div class="dir-content-grid">
            <div v-for="folderInfo of filteredFolderInfos" :class="{selected: selectedFolderInfos.includes(folderInfo)}">
                <Tile :folderInfo="folderInfo" :allowChanges="!isBulkEdit" @click="handleFolderClick(folderInfo)" @changed="refresh"></Tile>
            </div>
            <div v-for="mediaInfo of filteredMediaInfos" :class="{selected: selectedMediaInfos.includes(mediaInfo)}">
                <Tile :mediaInfo="mediaInfo" :allowChanges="!isBulkEdit" @click="handleMediaClick(mediaInfo)" @changed="refresh"></Tile>
            </div>
        </div>
        <div v-if="!isBulkEdit" style="display: flex; justify-content: end; gap: 12px;">
            <button @click="newFolder">New Folder</button>
            <button @click="startBulkEdit">Bulk Edit</button>
            <UploadFromUrl @uploaded="refresh"></UploadFromUrl>
            <UploadFileModal @uploaded="refresh"></UploadFileModal>
        </div>
        <div v-if="isBulkEdit" style="height: 64px;"></div>
        <div v-if="isBulkEdit" class="bulk-edit-footer">
            <div style="display: flex; justify-content: space-between;">
                <h3 style="margin: 8px 0 4px 8px;">Bulk Edit Mode ({{ (selectedFolderInfos.length + selectedMediaInfos.length) + '/' + (filteredFolderInfos.length + filteredMediaInfos.length) }})</h3>
                <div style="padding: 8px 8px 0 0;"><button @click="stopBulkEdit">Cancel</button></div>
            </div>
            <div style="margin-left: 8px">
                <button @click="toggleSelectAll">Toggle Select All</button>
                <button @click="bulkCopy(true)" style="margin-left: 40px;">Move</button>
                <button @click="bulkCopy(false)" style="margin-left: 8px;">Copy</button>
                <button @click="bulkDelete" style="margin-left: 8px;">delete</button>
            </div>
        </div>
    </div>
</template>

<style>
.bulk-edit-footer{
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    height: 64px;
    background-color: var(--bulk-select-color);
    box-shadow: 0 0 8px 0 rgba(0,0,0,0.4);
}

.selected {
    --tile-bg: var(--bulk-select-color)
}

.sort-btn-container{
    position: absolute;
    bottom: 100%;
    right: 8px;
}

.btn{
    cursor: pointer;
    outline: none;
    border: none;
    color: var(--sort-btn-color);
    background: none;
    padding: 4px 8px;
    border-radius: 3px;;
    margin-bottom: 2px;
}
.btn:hover{
    color: var(--sort-btn-hover-color);
    background: var(--sort-btn-hover-bg);
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
    padding: 0 10px 30px 10px
}

@media (min-width: 1200px) {
    .dir-content-grid{
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        max-width: 1600px;
    }
}

</style>