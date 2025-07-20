<script setup lang="ts">

import { watch, shallowRef, type ShallowRef, ref, onUnmounted, onMounted, computed } from 'vue';
import { apiAccess } from '@/services/ApiAccess';
import { pathService } from '@/services/PathService';
import { modalService } from '@/services/ModalService';
import type { FolderInfo, MediaInfo } from '@/models/models';
import { settingsService } from '@/services/SettingsService';
import UploadFileModal from './UploadFileModal.vue';
import UploadFromUrl from './UploadUrlModal.vue'
import BreadCrumbs from './BreadCrumbs.vue';
import Tile from './Tile.vue';
import { fileTypeHelper } from '@/services/FileTypeHelper';
import { dragService } from '@/services/DragService';

function handleMediaClick(mediaInfo: MediaInfo) {
    if (!isBulkEdit.value){
        return //when not bulk editing, we use an <a> for navigation
    } 
    if (selectedMediaInfos.value.includes(mediaInfo)){
        selectedMediaInfos.value = selectedMediaInfos.value.filter(z => z != mediaInfo);
    } else {
        selectedMediaInfos.value = [...selectedMediaInfos.value, mediaInfo]
    }
}

function handleFolderClick(folderInfo: FolderInfo) {
    if (!isBulkEdit.value){
        return //when not bulk editing, we use an <a> for navigation
    } 
    if (selectedFolderInfos.value.includes(folderInfo)){
        selectedFolderInfos.value = selectedFolderInfos.value.filter(z => z != folderInfo);
    } else {
        selectedFolderInfos.value = [...selectedFolderInfos.value, folderInfo]
    }
}
function getFolderUrl(folderInfo: FolderInfo): string | undefined {
    if (isBulkEdit.value){
        return undefined;
    }
    return pathService.getAppendedUrl(folderInfo.folderName);
 }
 function getMediaUrl(mediaInfo: MediaInfo): string | undefined {
    if (isBulkEdit.value){
        return undefined;
    }
    return pathService.getAppendedUrl(mediaInfo.fileName);
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
var isFilterLoading = ref<boolean>(false);
var isLoading = ref<boolean>(false);
var filterText = ref<string>("");
var sortBy = ref<string>("name");
var foldersFirst = ref<boolean>(false);
var filterChangedTimeout: number | undefined;

var isAdvancedFilter = ref<boolean>(false);
var anyOrder = ref<boolean>(false);
var regex = ref<boolean>(false);
var typeTolerance = ref<boolean>(false);
var folderContents = ref<boolean>(false);


watch(pathService.getPath(), (newVal, oldVal) => {
    isFilterLoading.value = false;
    load();
});
modalService.registerOnRefresh(refresh);

var computedSortCombined = computed(() => {
    const combined = [
        ...filteredFolderInfos.value,
        ...filteredMediaInfos.value
    ];
    if (sortBy.value == "modified"){
        combined.sort((z1, z2) => new Date(z2.modifyDate).getTime() - new Date(z1.modifyDate).getTime())
    }
    else if (sortBy.value == "duration"){
        combined.sort((z1, z2) => ((<any>z2).duration != null ? (<any>z2).duration : 0) - ((<any>z1).duration != null ? (<any>z1).duration : 0))
    }    
    else if (sortBy.value == "size"){
        combined.sort((z1, z2) => ((<any>z2).fileSize != null ? (<any>z2).fileSize : (<any>z2).mediaDiskSize) - ((<any>z1).fileSize != null ? (<any>z1).fileSize : (<any>z1).mediaDiskSize))
    }
    else if (sortBy.value == "random"){
        var randSort = (_: any, i: number, arr: any[]) => { const j = Math.floor(Math.random() * (i + 1)); [arr[i], arr[j]] = [arr[j], arr[i]]; };
        combined.forEach(randSort);
    }
    else {
        combined.sort((z1, z2) => ((<any>z1).folderName != null ? (<any>z1).folderName : (<any>z1).fileName).localeCompare((<any>z2).folderName != null ? (<any>z2).folderName : (<any>z2).fileName))
    }
    if (!sortDesc.value){
        combined.reverse();
    }
    return combined.map(z => {
        return {
            folder: (<any>z).folderName != null ? <FolderInfo>z : null,
            media: (<any>z).fileName != null ? <MediaInfo>z : null
        }
    })
})

async function load() {
    if (!pathService.isFile().value) {
        var pathString = pathService.getPathString();
        folderInfos.value = [];
        mediaInfos.value = [];
        selectedFolderInfos.value = [];
        selectedMediaInfos.value = [];
        isBulkEdit.value = false;
        isLoading.value = true;
        var result = await apiAccess.getDirContents(pathString);
        if (result === null){
            pathService.setIsFile();
            return;
        }
        var settings = await settingsService.getSettingsAsync();
        sortBy.value = settings.sortBy;
        foldersFirst.value = settings.foldersFirst
        sortDesc.value = settings.sortDesc;
        folderInfos.value = result.folderInfos
        mediaInfos.value = result.mediaInfos
        sort();
        if (result.mediaInfos.some(z => z.duration == null && fileTypeHelper.isMedia(z.fileName))) {
            loadDurations(pathString);
        }
        await updateFilter();
        isLoading.value = false;
    }
}
load();

async function loadDurations(pathString: string, ){
    var durationsResponse = await apiAccess.getDurations(pathString);
    if (pathString != pathService.getPathString()){
        //path must have changed
        return;
    }
    var updatedMediaInfos = mediaInfos.value.map(mediaInfo => {
        var foundDuration = durationsResponse.mediaDurations.find(z => z.fileName == mediaInfo.fileName);
        if (foundDuration != null){
            mediaInfo.duration = foundDuration.duration;
        }
        return {...mediaInfo}
    });
    mediaInfos.value = updatedMediaInfos
    updateFilter();
}

function toggleFoldersFirst(){
    foldersFirst.value = !foldersFirst.value;
    setTimeout(() => {
        showDropdown.value = false;
    }, 150)
    settingsService.updateSort(sortBy.value, sortDesc.value, foldersFirst.value);
    sort();
    cleanUpSortChange();
}

function changeSortBy(newSortBy: string){
    sortBy.value = newSortBy;
    setTimeout(() => {
        showDropdown.value = false;
    }, 150)
    settingsService.updateSort(sortBy.value, sortDesc.value, foldersFirst.value);
    sort();
    cleanUpSortChange();
}
function toggleSortDesc(){
    sortDesc.value = !sortDesc.value
    settingsService.updateSort(sortBy.value, sortDesc.value, foldersFirst.value);
    folderInfos.value.reverse();
    mediaInfos.value.reverse();
    cleanUpSortChange();
}
function cleanUpSortChange(){
    filteredFolderInfos.value = folderInfos.value.filter(z => filteredFolderInfos.value.some(zz => zz.folderName == z.folderName));
    filteredMediaInfos.value =  mediaInfos.value.filter(z => filteredMediaInfos.value.some(zz => zz.fileName == z.fileName));
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
function bodyKeyDownHandler(e: KeyboardEvent){
    const isInputFocused = document.activeElement && document.activeElement.tagName === 'INPUT';
    if (isInputFocused){
        return;
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'a') {
        startBulkEdit();
        selectedFolderInfos.value = [...filteredFolderInfos.value]
        selectedMediaInfos.value = [...filteredMediaInfos.value];
        e.preventDefault();
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'c' && isBulkEdit.value) {
        bulkCopy(false);
        e.preventDefault();
    }
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'x' && isBulkEdit.value) {
        bulkCopy(true);
        e.preventDefault();
    }
    console.log(e.key)
    if (e.key.toLowerCase() === 'delete' && isBulkEdit.value) {
        bulkDelete();
        e.preventDefault();
    }
}


onMounted(() => {
    document.body.addEventListener("click", bodyClickHandler);
    document.body.addEventListener("keydown", bodyKeyDownHandler);
    document.title = "Fileplayer";
    dragService.subscribeMove(refresh);
})
onUnmounted(() => {
    document.body.removeEventListener("click", bodyClickHandler);
    document.body.removeEventListener("keydown", bodyKeyDownHandler);
    dragService.unsubscribeMove(refresh)
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
    else if (sortBy.value == "random"){
        var randSort = (_: any, i: number, arr: any[]) => { const j = Math.floor(Math.random() * (i + 1)); [arr[i], arr[j]] = [arr[j], arr[i]]; };
        folderInfos.value.forEach(randSort);
        mediaInfos.value.forEach(randSort);
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

function toggleIsAdvanced(){
    isAdvancedFilter.value = !isAdvancedFilter.value;
    updateFilter();
}

function clearFilter(){
    filterText.value = "";
    filterChanged();
}

function filterChanged(){
    clearTimeout(filterChangedTimeout);
    if (filterText.value){
        filterChangedTimeout = setTimeout(() => {
            updateFilter();
        }, isAdvancedFilter.value ? 500 : 150)
        isFilterLoading.value = !!isAdvancedFilter.value;
    } else {
        updateFilter();
    }
}

async function updateFilter(){
    if (isFilter.value){
        if (isAdvancedFilter.value && filterText.value){
            isFilterLoading.value = true;
            var prevPath = pathService.getPathString();
            var prevFilter = filterText.value;
            var result = await apiAccess.searchDirContents(pathService.getPathString(), filterText.value, folderContents.value, typeTolerance.value, anyOrder.value, regex.value);
            if (prevFilter != filterText.value || prevPath != pathService.getPathString()){
                return; 
            }
            isFilterLoading.value = false;
            filteredFolderInfos.value = folderInfos.value.filter(z => result.folderInfos.some(zz => zz.folderName == z.folderName));
            filteredMediaInfos.value = mediaInfos.value.filter(z => result.mediaInfos.some(zz => zz.fileName == z.fileName));
        } else {
            var lower = filterText.value.toLowerCase();
            filteredFolderInfos.value = folderInfos.value.filter(z => z.folderName.toLowerCase().includes(lower));
            filteredMediaInfos.value = mediaInfos.value.filter(z => z.fileName.toLowerCase().includes(lower));
        }
        selectedFolderInfos.value =  selectedFolderInfos.value.filter(z => filteredFolderInfos.value.some(zz => zz.folderName == z.folderName));
        selectedMediaInfos.value = selectedMediaInfos.value.filter(z => filteredMediaInfos.value.some(zz => zz.fileName == z.fileName));
    } else {
        filteredFolderInfos.value = folderInfos.value
        filteredMediaInfos.value = mediaInfos.value
    }
    isFilterLoading.value = false;
}

function refresh(){
    load();
}

</script>

<template>
    <div class="top-section-grid">
        <div style="grid-area: breadcrumbs; min-width: 0;">
            <BreadCrumbs></BreadCrumbs>
        </div>
        <div style="grid-area: filtersort;">
            <div style="position: relative; display: flex; justify-content: end; margin-top: 2px;">
                <button class="btn" @click="toggleIsFilter">filter</button>
                <div @click="stopPropogate">
                    <button class="btn" @click="showDropdown = !showDropdown" style="margin-right: -4px;">
                        sort by
                        <div class="context-menu checkable" :class="{active: showDropdown}">
                            <div class="menu-item" @click="changeSortBy('name')"><span v-if="sortBy=='name'">✓</span>name</div>
                            <div class="menu-item" @click="changeSortBy('modified')"><span v-if="sortBy=='modified'">✓</span>date modified</div>
                            <div class="menu-item" @click="changeSortBy('duration')"><span v-if="sortBy=='duration'">✓</span>duration</div>
                            <div class="menu-item" @click="changeSortBy('size')"><span v-if="sortBy=='size'">✓</span>size</div>
                            <div class="menu-item" @click="changeSortBy('random')"><span v-if="sortBy=='random'">✓</span>random</div>
                            <div class="menu-sep"></div>
                            <div class="menu-item" @click="toggleFoldersFirst()"><span v-if="foldersFirst">✓</span>folders first</div>
                        </div>
                    </button>
                </div>
                <button class="btn" style="font-family: monospace" @click="toggleSortDesc">{{ sortDesc ? "↓" : "↑" }}</button>
            </div>
        </div>
        <div v-if="isFilter" class="text-muted" style="grid-area: filterCount; margin-left: 8px;">
            <span v-if="filterText">filtered to {{  filteredFolderInfos.length + filteredMediaInfos.length}} of {{ folderInfos.length + mediaInfos.length}}</span>
        </div>
        <div v-if="isFilter" class="text-muted" style="grid-area: advancedFilter; margin: 4px 8px; display: flex; flex-wrap: wrap; justify-content: end; gap: 12px;">
            <template v-if="isAdvancedFilter">
                <label style="white-space: nowrap;" :class="{'text-very-muted': regex}">
                    <input type="checkbox" v-model="anyOrder" @change="filterChanged" :disabled="regex">
                    match words in any order
                </label>
                <label style="white-space: nowrap;" :class="{'text-very-muted': regex}">
                    <input type="checkbox" v-model="typeTolerance" @change="filterChanged" :disabled="regex">
                    typo tolerance
                </label>
                <label style="white-space: nowrap;">
                    <input type="checkbox" v-model="regex" @change="filterChanged">
                    regex
                </label>
                <label style="white-space: nowrap;">
                    <input type="checkbox" v-model="folderContents" @change="filterChanged">
                    consider folder contents
                </label>
            </template>
        </div>
        <div v-if="isFilter" style="grid-area: filterInput; margin-bottom: 6px; margin-right: 8px;">
            <div style="position: relative; margin-right: 4px; display: inline-block;">
                <input id="filter-input" placeholder="enter filter text" v-model="filterText" @input="filterChanged()" autocomplete="off">
                <div v-if="filterText" class="clear-btn" @click="clearFilter">&times;</div>
            </div>
            <button @click="toggleIsAdvanced">advanced</button>
        </div>
    </div>
    <div>
        <div class="dir-content-grid" v-if="!isLoading && !isFilterLoading">
            <template v-if="foldersFirst">
                <div v-for="folderInfo of filteredFolderInfos" :class="{selected: selectedFolderInfos.includes(folderInfo)}">
                    <Tile :folderInfo="folderInfo" :allowChanges="!isBulkEdit" @click="handleFolderClick(folderInfo)" @changed="refresh" :url="getFolderUrl(folderInfo)"></Tile>
                </div>
                <div v-for="mediaInfo of filteredMediaInfos" :class="{selected: selectedMediaInfos.includes(mediaInfo)}">
                    <Tile :mediaInfo="mediaInfo" :allowChanges="!isBulkEdit" @click="handleMediaClick(mediaInfo)" @changed="refresh" :url="getMediaUrl(mediaInfo)"></Tile>
                </div>
            </template>
            <template v-if="!foldersFirst">
                <template v-for="info of computedSortCombined">
                    <div v-if="info.folder" :class="{selected: selectedFolderInfos.includes(info.folder)}" >
                        <Tile :folderInfo="info.folder" :allowChanges="!isBulkEdit" @click="handleFolderClick(info.folder)" @changed="refresh" :url="getFolderUrl(info.folder)"></Tile>
                    </div>
                    <div v-if="info.media" :class="{selected: selectedMediaInfos.includes(info.media)}">
                        <Tile :mediaInfo="info.media" :allowChanges="!isBulkEdit" @click="handleMediaClick(info.media)" @changed="refresh" :url="getMediaUrl(info.media)"></Tile>
                    </div>
                </template>
            </template>
        </div>
        <div class="center-message fade-in" v-if="isLoading || isFilterLoading">
            <h2>loading</h2>
        </div>
        <div class="center-message" v-if="!isLoading && !isFilterLoading && !filteredFolderInfos.length && !filteredMediaInfos.length">
            <h2 v-if="filterText && (mediaInfos.length || folderInfos.length)">No Items Match Filter</h2>
            <h2 v-else>Folder is Empty</h2>
        </div>
        <div v-if="!isBulkEdit" style="height: 32px;"></div>
        <div v-if="!isBulkEdit" class="footer-buttons">
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
            <div style="margin-left: 8px;">
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
    box-shadow: 0 0 8px 1px rgba(0,0,0,0.5);
    z-index: 50;
}

.footer-buttons{
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    height: 36px;
    display: flex; 
    justify-content: end; 
    align-items: center ;
    gap: 4px; 
    background: var(--bg-default);
    z-index: 50;
    border-top: 1px solid var(--border-default)
}

.selected {
    --tile-bg: var(--bulk-select-color)
}

.center-message{
    padding-top: 64px;
    display: flex;
    justify-content: center;
    align-items: center;
    color: var(--text-muted)
}
.fade-in{
    animation: loadingfadein 1s;
}

@keyframes loadingfadein {
    0% {
        opacity: 0;
    }
    100% {
        opacity: 1;
    }
}

.btn{
    background: var(--bg-default);
    cursor: pointer;
    outline: none;
    border: none;
    color: var(--sort-btn-color);
    padding: 4px 8px;
    border-radius: 3px;;
    margin-bottom: 2px;
}
.btn:hover{
    color: var(--sort-btn-hover-color);
    background: var(--sort-btn-hover-bg);
}

.dir-content-grid{
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    gap: 10px;
    max-width: 800px;
    margin: 0 auto;
    padding: 4px 10px 30px 10px
}

.clear-btn{
    position: absolute;
    right: 0;
    top: 0px;
    padding: 2px 8px;
    color: var(--text-muted);
    cursor: pointer;
}
.clear-btn:hover{
    color: var(--text-default);
}

@media (min-width: 1200px) {
    .dir-content-grid{
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
        max-width: 1600px;
    }
    .footer-buttons{
        gap: 12px;
    }
}

.top-section-grid{
        display: grid;
        gap: 0 8px;
        grid-template-columns: 1fr auto;
        grid-template-rows: auto auto auto;
        grid-template-areas:
            "breadcrumbs breadcrumbs"
            ". filtersort"
            "advancedFilter advancedFilter"
            "filterCount filterInput";
    }

@media (min-width: 1200px) {
    .top-section-grid{
        grid-template-columns: 1fr auto;
        grid-template-rows: auto auto;
        grid-template-areas:
            "breadcrumbs filtersort"
            "advancedFilter advancedFilter"
            "filterCount filterInput";
    }

}


</style>