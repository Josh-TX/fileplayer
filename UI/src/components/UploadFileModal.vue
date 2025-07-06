<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, shallowRef } from 'vue';
import { pathService } from '@/services/PathService';
import { apiAccess } from '@/services/ApiAccess';
import { fileDropService } from '@/services/FileDropService';

var emits = defineEmits(['uploaded'])
const open = ref(false)
const fileInput = ref<HTMLInputElement | null>(null);
const files = shallowRef<File[]>([])
const queuedFiles = shallowRef<File[]>([])
const uploadingFile = shallowRef<File | null>(null)
const finishedFiles = shallowRef<File[]>([])


const displayPath = computed(() => pathService.getPathString() || "root");
function filesAdded(addedFiles: File[]) {
    files.value = addedFiles;
}

async function upload() {
    if (queuedFiles.value.length){
        alert("file upload already in progress")
        return;
    }
    queuedFiles.value = files.value
    files.value = []
    var queuedFilesCopy = [...queuedFiles.value].reverse();
    for (var file of queuedFilesCopy){
        queuedFiles.value = queuedFiles.value.slice(0, queuedFiles.value.length - 1);
        uploadingFile.value = file;
        await apiAccess.uploadFiles(file, pathService.getPathString());
        finishedFiles.value = [...finishedFiles.value, uploadingFile.value];
    }
    uploadingFile.value = null;
    emits("uploaded");
}

function close(){
    files.value = [];
    open.value = false;
}

function onDrop(droppedFiles: File[]){
    open.value = true;
    files.value = droppedFiles;
}

function browseFiles() {
    if (fileInput.value) {
        fileInput.value.click();
    }
};

function handleFileSelect(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files){
        files.value = Array.from(input.files);
    }
};

onMounted(() => {
    fileDropService.subscribe(onDrop);
})

onUnmounted(() => {
    fileDropService.unsubscribe(onDrop);
})

</script>

<template>
    <div style="display: flex; justify-content: end; padding-right: 12px;">
        <button @click="open = true">Upload Files</button>
    </div>
    <div class="modal-overlay" v-if="open">
        <div class="modal-container">
            <div class="modal">
                <h2 style="margin: 0 0 4px 0;">upload files to {{ displayPath }}</h2>
                <div style="display: flex; justify-content: center; text-align: center; padding: 24px 8px">
                    <div>
                        <h3 style="margin-bottom: 0">Drag files here</h3>
                        <div style="margin: 8px 0">or</div>
                        <button @click="browseFiles">browse files</button>
                    </div>
                </div>
                <input type="file" ref="fileInput" @change="handleFileSelect" multiple style="display: none;" />
                <template v-if="files.length">
                    <label>Ready to Upload</label>
                    <div style="max-height: 300px; overflow: auto;">
                        <div v-for="file of files" class="upload-tile">
                            {{ file.name }}
                        </div>
                    </div>
                </template>
                <template v-if="queuedFiles.length">
                    <label>Queued to Upload</label>
                    <div style="max-height: 300px; overflow: auto;">
                        <div v-for="file of queuedFiles" class="upload-tile">
                            {{ file.name }}
                        </div>
                    </div>
                </template>
                <template v-if="uploadingFile">
                    <label>Uploading</label>
                    <div style="max-height: 300px; overflow: auto;">
                        <div class="upload-tile">
                            {{ uploadingFile.name }}
                        </div>
                    </div>
                </template>
                <template v-if="finishedFiles.length">
                    <label>Finished Uploading</label>
                    <div style="max-height: 300px; overflow: auto;">
                        <div v-for="file of finishedFiles" class="upload-tile">
                            {{ file.name }}
                        </div>
                    </div>
                </template>
                <div style="display: flex; justify-content: space-between; margin-top: 6px;">
                    <button @click="close">close</button>
                    <button :disabled="!files.length || !!queuedFiles.length" @click="upload" >upload</button>
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

.upload-tile{
    margin: 8px 0;
    padding: 8px 8px;
    background-color: var(--tile-bg);
    box-shadow: var(--tile-shadow);
}

.modal-container {
    max-width: 800px;
    margin: 15vh auto 0 auto;
    padding: 0 12px;
}

.modal {
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