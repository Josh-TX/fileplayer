<script setup lang="ts">
import { ref } from 'vue';
import FolderList from './components/FolderList.vue';
import { pathService } from '@/services/PathService';
import BreadCrumbs from './components/BreadCrumbs.vue';
import MediaPlayer from './components/MediaPlayer.vue';
import FolderSelectModal from './components/FolderSelectModal.vue'
import { fileDropService } from './services/FileDropService';
console.log("Made by Josh-TX")
console.log("https://github.com/Josh-TX/fileplayer")


const isDragging = ref(false);
function dragEnter(event: DragEvent) {
    console.log("dragEnter")
    event.preventDefault();
    isDragging.value = true;
};
function dragLeave(event: DragEvent) {
    event.preventDefault();
    var relatedTarget = event.relatedTarget //|| event.fromElement;
    var dropArea = document.getElementById("app-main");
    if (!dropArea || !dropArea!.contains(<any>relatedTarget)) {
        isDragging.value = false;
    }
};
function handleDrop(event: DragEvent){
    const files = event.dataTransfer?.files;
    if (files) {
        fileDropService.filesDropped(Array.from(files));
    }
    isDragging.value = false;
};

</script>

<template>
    <div id="app-main" style="display: flex; flex-direction: column; height: 100%;" @dragover.prevent @dragenter.prevent="dragEnter" @dragleave="dragLeave" @drop.prevent="handleDrop">
        <FolderList v-if="!pathService.isFile().value"></FolderList>
        <MediaPlayer v-if="pathService.isFile().value"></MediaPlayer>
        <div :class="['file-drop-overlay', { 'drag-over': isDragging && !pathService.isFile().value }]">
            <div class="inner-overlay">
                Drop Files Here
            </div>
        </div>
    </div>
    <FolderSelectModal></FolderSelectModal>
</template>

<style scoped>
header {
    line-height: 1.5;
}

.logo {
    display: block;
    margin: 0 auto 2rem;
}

.file-drop-overlay {
    position: fixed;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    background: rgba(0,0,0,0.75);
    opacity: 0;
    transition: opacity 0.3s;
    z-index: 1000;
    padding: 3vh 3vw;
    pointer-events: none;
}
.inner-overlay {
    box-sizing: border-box;
    height: 100%;
    width: 100%;
    border: 5px dashed var(--border-filedrop);
    border-radius: 40px;
    display: flex;
    justify-content: center;
    align-items: center;
    font-size: 48px;
    font-weight: bold;
}
.file-drop-overlay.drag-over {
    opacity: 1;
}


@media (min-width: 1024px) {
    header {
        display: flex;
        place-items: center;
        padding-right: calc(var(--section-gap) / 2);
    }

    .logo {
        margin: 0 2rem 0 0;
    }

    header .wrapper {
        display: flex;
        place-items: flex-start;
        flex-wrap: wrap;
    }
}
</style>
