<script setup lang="ts">
import type { FolderInfo } from '@/models/models';
import { computed } from 'vue';

const props = defineProps<{
    folderInfo: FolderInfo
}>()

const computedDate = computed(() => {
    var date = new Date(props.folderInfo.modifyDate);
    return new Intl.DateTimeFormat('en-US').format(date);
})

const computedSize = computed(() => {
    var bytes = props.folderInfo.mediaDiskSize;
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
})

</script>

<template>

    <div class="tile-container">
        <div class="left-side">
            <img src="/folder-icon.png">
        </div>
        <div class="right-side">
            <h2 class="foldername">{{ folderInfo.folderName }}</h2>
            <div class="info-container">
                <div>{{ computedSize }}</div>
                <div class="modify-date">{{ computedDate }}</div>
                <div>{{ folderInfo.mediaFileCount }}</div>
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

.foldername {
    overflow: hidden;
    width: 100%;
    min-width: 100%;
    margin: 1px 0 0 0;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.info-container{
    display: flex;
    position: relative;
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