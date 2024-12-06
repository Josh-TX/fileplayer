<script setup lang="ts">

import { watch, shallowRef, type ShallowRef } from 'vue';
import { apiAccess } from '@/services/ApiAccess';
import FolderItem from './FolderItem.vue';
import { pathService } from '@/services/PathService';

function handleFileClick(fileName: string) {
  pathService.appendFile(fileName);
}

// Emit "folder-clicked" event with a string payload
function handleFolderClick(folderName: string) {
  pathService.appendFolder(folderName);
}

var folders: ShallowRef<string[]> = shallowRef([]);
var files: ShallowRef<string[]> = shallowRef([]);

watch(pathService.getWatcherPathRef(),(newVal, oldVal) => {
  load();
});

async function load(){
  if (!pathService.isFile().value){
    var result = await apiAccess.getDirContents(pathService.getPathString());
    folders.value = result.directories
    files.value = result.mediaInfos.map(z => z.fileName)
  }
}
load();

</script>

<template>
    <div v-for="folder of folders">
        <FolderItem :name="folder" @click="handleFolderClick(folder)"></FolderItem>
    </div>
    <div v-for="file of files">
        <FolderItem :name="file" @click="handleFileClick(file)"></FolderItem>
    </div>
</template>
