<script setup lang="ts">
import type { MediaInfo } from '@/models/models';
import { computed } from 'vue';

const props = defineProps<{
    mediaInfo: MediaInfo
}>()

const computedExt = computed(() => {
    return props.mediaInfo.fileName.split('.').pop();
});

const computedSize = computed(() => {
    var bytes = props.mediaInfo.fileSize;
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

const computedDate = computed(() => {
    var date = new Date(props.mediaInfo.modifyDate);
    return new Intl.DateTimeFormat('en-US').format(date);
})

const computedDuration = computed(() => {
    var secondsCount = props.mediaInfo.duration;
    if (secondsCount == null){
        return "\xa0\xa0"
    }
    const hours = Math.floor(secondsCount / 3600);
    const minutes = Math.floor((secondsCount % 3600) / 60);
    const seconds = secondsCount % 60;

    return [
        hours > 0 ? String(hours) : null, // Include hours only if greater than 0
        String(minutes).padStart(hours > 0 ? 2 : 1, '0'), // Ensure minutes are padded if hours are included
        String(seconds).padStart(2, '0') // Always pad seconds
    ].filter(Boolean).join(':'); // Filter out `null` and join with colons
});
</script>

<template>
    <div class="tile-container">
        <div class="left-side">
            <div class="ext">{{ computedExt }}</div>
            <img src="/file-icon.png">
        </div>
        <div class="right-side">
            <h2 class="filename">{{ mediaInfo.fileName }}</h2>
            <div class="info-container">
                <div>{{ computedSize }}</div>
                <div class="modify-date">{{ computedDate }}</div>
                <div>{{ computedDuration }}</div>
            </div>
        </div>
        <div v-if="mediaInfo.progress != null" class="progress" :style="{width: (mediaInfo.progress*100)+'%'}"></div>
        <div v-if="mediaInfo.progress != null" class="progress progress-gray"></div>
    </div>
</template>

<style scoped>


.tile-container {
    cursor: pointer;
    height: 48px;
    display: flex;
    background: #222;
    position: relative;
    box-shadow: 2px 2px 3px rgba(0,0,0,0.2);
}
.progress{
    position: absolute;
    z-index: 2;
    left: 0;
    bottom: 0;
    height: 3px;
    background-color: rgb(0, 183, 255);
}

.progress-gray{
    background-color: rgb(100,100,100);
    z-index: 1;
    width: 100%;
}

.left-side {
    position: relative;
    width: 36px;
    min-width: 36px;
    padding: 6px;
    text-align: right;
}

.left-side>img {
    height: 36px;
}

.ext {
    position: absolute;
    top: 18px;
    padding: 2px;
    font-size: 14px;
    text-transform: uppercase;
    border-radius: 3px;
    background-color: rgb(0, 183, 255);
    color: white;
    box-shadow: 1px 1px 2px rgba(0,0,0,0.2);
}

.right-side{
    flex: 1 0 0;
    overflow: hidden;;
}

.filename {
    overflow: hidden;
    width: 100%;
    min-width: 100%;
    margin: 1px 0 0 0;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.info-container{
    position: relative;
    display: flex;
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