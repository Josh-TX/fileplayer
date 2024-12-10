<script setup lang="ts">
import { ref } from 'vue';

var emits = defineEmits(['filesAdded'])
const fileInput = ref<HTMLInputElement | null>(null);
const isDragging = ref(false);

function dragEnter(event: DragEvent) {
    event.preventDefault();
    isDragging.value = true;
};

function dragLeave(event: DragEvent) {
    event.preventDefault();
    var relatedTarget = event.relatedTarget //|| event.fromElement;
    var dropArea = document.querySelector(".file-drop-area");
    if (!dropArea!.contains(<any>relatedTarget)) {
        isDragging.value = false;
    }
};

function handleDrop(event: DragEvent){
    const files = event.dataTransfer?.files;
    if (files) {
        processFiles(files);
    }
    isDragging.value = false;
};

function handleFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    const files = input.files;
    if (files) {
        processFiles(files);
    }
};

function browseFiles() {
    if (fileInput.value) {
        fileInput.value.click();
    }
};

function processFiles(files: FileList) {
    var fileArray = Array.from(files);
    emits("filesAdded", fileArray);
};
</script>

<template>
    <div>

    </div>
    <div :class="['file-drop-area', { 'drag-over': isDragging }]" @dragover.prevent @dragenter.prevent="dragEnter"
        @dragleave="dragLeave" @drop.prevent="handleDrop">
        <div v-if="!isDragging">
            <h2 style="margin-bottom: 0">Drag files here</h2>
            <div style="margin: 8px 0">or</div>
            <button v-if="!isDragging" @click="browseFiles">browse files</button>
        </div>
        <div v-if="isDragging">Drop files here</div>
        <input type="file" ref="fileInput" @change="handleFileSelect" multiple style="display: none;" />
    </div>
</template>

<style scoped>
.file-drop-area {
    border: 2px dashed #ccc;
    height: 20vh;
    min-height: 300px;
    text-align: center;
    display: flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    transition: border-color 0.3s;
    width: 100%;
}

.file-drop-area.drag-over {
    border-color: #44AAFF;
}

.file-drop-area p {
    margin: 0;
}
</style>