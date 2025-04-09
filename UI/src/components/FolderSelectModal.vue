<script setup lang="ts">
import { computed, ref, shallowRef, watch } from 'vue';
import { modalService } from '../services/ModalService'
import { apiAccess } from '../services/ApiAccess'
import type { FolderInfo, MediaInfo } from '@/models/models';
import Tile from './Tile.vue';

var folderInfos = shallowRef<FolderInfo[]>([]);
var mediaInfos = shallowRef<MediaInfo[]>([]);
var crumbs = ref<string[]>([]);
function close(){
    modalService.isModalOpen.value = false;
}
var isModalOpen = modalService.isModalOpen;

watch(modalService.currentPath, (newVal, oldVal) => {
    crumbs.value = ["root", ...modalService.currentPath.value.filter(z => !!z)]
    load();
});

watch(modalService.isModalOpen, (newVal, oldVal) => {
    load();
});

async function load() {
    if (!modalService.isNewFolder.value){
        var pathString = modalService.currentPath.value.join("/");
        var result = await apiAccess.getDirContents(pathString);
        if (pathString != modalService.currentPath.value.join("/")){
            return; //path must have changed while loading
        }
        folderInfos.value = result.folderInfos;
        mediaInfos.value = result.mediaInfos;
        folderInfos.value.sort((z1, z2) => z1.folderName.localeCompare(z2.folderName))
        mediaInfos.value.sort((z1, z2) => z1.fileName.localeCompare(z2.fileName))
    } else {
        folderInfos.value = [];
        mediaInfos.value = [];
    }
    setTimeout(() => {
        var el = document.querySelector(".modal .breadcrumb");
        if (el){
            el.scrollLeft = el.scrollWidth - el.clientWidth;
        }
    })
}

function handleFolderClick(folderInfo: FolderInfo){
    modalService.currentPath.value = [...modalService.currentPath.value, folderInfo.folderName];
}
function breadcrumbClicked(index: number){
    modalService.isNewFolder.value = false;
    modalService.currentPath.value = modalService.currentPath.value.slice(0, index);
}
function newFolder(){
    var newFolderName = prompt("enter new folder name")
    if (newFolderName){
        modalService.isNewFolder.value = true;
        modalService.currentPath.value = [...modalService.currentPath.value, newFolderName];
    }
}

const title = computed(() => {
    var folderCount = modalService.folderCount.value;
    var fileCount = modalService.files.value.length - folderCount;
    var action = modalService.isMove.value ? 'Move ' : 'Copy ';
    var msg = action + (fileCount == 1 ? "1 file" : fileCount + " files");
    if (folderCount > 0){
        var msg = fileCount == 0 ? action : msg + " and ";
        msg += folderCount == 1 ? "1 folder" : folderCount + " folders";
    }
    return msg;
})

</script>

<template>
    <div class="modal-overlay" v-if="isModalOpen">
        <div class="modal-container">
            <div class="modal">
                <h2 style="margin: 0 0 4px 0;">{{ title }}</h2>
                <div><small>to</small></div>

                <!-- I copy pasted breadcrumb component code since the other component is too coupled to navigation-->
                <nav aria-label="breadcrumb">
                    <ol class="breadcrumb" style="padding-left: 0;">
                        <li v-for="(item, index) in crumbs" :key="index" class="breadcrumb-item">
                            <!-- If it's not the last item, make it a clickable link -->
                            <template v-if="index < crumbs.length - 1">
                                <a class="link" href="#" @click.prevent="breadcrumbClicked(index)">{{ item }}</a>
                            </template>
                            <!-- If it's the last item, just display text -->
                            <template v-else>
                                <span v-if="index != 0">&nbsp;</span>{{ item }}
                            </template>
                        </li>
                    </ol>
                </nav>

                <div style="margin: 8px 0; min-height: 50vh; max-height: 50vh; overflow-y: auto; overflow-x: hidden;">
                    <div v-for="folderInfo of folderInfos" style="margin: 5px 0;">
                        <Tile :folderInfo="folderInfo" @click="handleFolderClick(folderInfo)"></Tile>
                    </div>
                    <div v-for="mediaInfo of mediaInfos" style="margin: 5px 0;">
                        <Tile :mediaInfo="mediaInfo"></Tile>
                    </div>
                    <div v-if="!folderInfos.length" style="text-align: center; margin-top: 10vh;;">
                        <span v-if="!modalService.isNewFolder.value">folder has no subfolders</span>
                        <span v-if="modalService.isNewFolder.value">(new folder)</span>
                    </div>
                </div>
                <div style="display: flex; justify-content: space-between; margin-top: 6px;">
                    <button @click="newFolder">new folder</button>
                    <div>
                        <button @click="close" style="margin-right: 8px;">Cancel</button>
                        <button @click="modalService.save()" :disabled="modalService.isSaving.value">{{ modalService.isMove.value ? 'Move' : 'Copy '}}</button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
@keyframes fadeIn {
    0% {
        opacity: 0;
    }

    100% {
        opacity: 1;
    }
}

.modal-container {
    max-width: 800px;
    margin: 10vh auto 0 auto;
    padding: 0 12px;
    display: flex;
    justify-content: center;;
}
.clickable-text{
    cursor: pointer;
}
.clickable-text:hover {
    text-decoration: underline !important;
}

.bigger-font{
    font-size: 1.2em; 
    font-weight: 600;
}

.modal {
    width: 100%;
    background: var(--bg-default);
    border: 1px solid var(--dropdown-border);
    padding: 8px 16px 16px 16px;
    box-shadow: 4px 4px 16px 2px rgba(0, 0, 0, 0.5);
}

.modal-overlay {
    position: fixed;
    z-index: 100;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background-color: rgba(0, 0, 0, 0.4);
    animation: fadeIn 0.4s;
}
</style>