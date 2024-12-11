<script setup lang="ts">
import { ref, shallowRef } from 'vue';
import FileDrop from './FileDrop.vue';
import { pathService } from '@/services/PathService';
import { apiAccess } from '@/services/ApiAccess';

var emits = defineEmits(['uploaded'])
const open = ref(false)
const files = shallowRef<File[]>([])
const displayPath = pathService.getPathString() || "root";
function filesAdded(addedFiles: File[]) {
    files.value = addedFiles;
}

async function upload() {
    for (var file of files.value){ 
        await apiAccess.uploadFiles(file, pathService.getPathString());
    }
    emits("uploaded");
    close();
}

function close(){
    files.value = [];
    open.value = false;
}

</script>

<template>
    <div style="display: flex; justify-content: end; padding-right: 12px;">
        <button @click="open = true">Upload Files</button>
    </div>
    <div class="modal-overlay" v-if="open">
        <div class="modal-container">
            <div class="modal">
                <h2 style="margin: 0 0 4px 0;">upload files to {{ displayPath }}</h2>
                <FileDrop @filesAdded="filesAdded"></FileDrop>
                <div style="max-height: 300px; overflow: auto;">
                    <div v-for="file of files" style="margin-top: 6px;">
                        {{ file.name }}
                    </div>
                </div>
                <div style="display: flex; justify-content: space-between; margin-top: 6px;">
                    <button @click="close">close</button>
                    <button :disabled="!files.length" @click="upload">upload</button>
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
    margin: 15vh auto 0 auto;
    padding: 0 12px;
}

.modal {
    background: #333;
    border: 1px solid #666;
    padding: 8px 16px 16px 16px;
    box-shadow: 4px 4px 16px 2px rgba(0, 0, 0, 0.5);
}

.modal-overlay {
    position: fixed;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    background-color: rgba(0, 0, 0, 0.4);
    animation: fadeIn 0.4s;
}
</style>