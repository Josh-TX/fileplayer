<script setup lang="ts">
import { pathService } from '@/services/PathService';
import { mediaService } from '@/services/MediaService';
import { onMounted, ref, shallowRef, watch } from 'vue';
import type { MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
const baseUrl = import.meta.env.VITE_BASE_URL;

var src = ref<string | null>(null)
var mediaInfo = shallowRef<MediaInfo | null>(pathService.getMediaInfo());
var mounted = false;
watch(pathService.getWatcherPathRef(),(newVal, oldVal) => {
    updateSrc();
});
function updateSrc(){
    src.value = baseUrl + "/data/" + pathService.getPathString();
}
updateSrc();
if (!mediaInfo.value){
    apiAccess.getMediaInfo(pathService.getPathString()).then(z => {
        mediaInfo.value = z
        if (mounted){
            mediaService.setup(pathService.getPathString(), mediaInfo.value.progress)
        }
    });
}
onMounted(() => {
    mounted = true;
    if (mediaInfo.value){
        mediaService.setup(pathService.getPathString(), mediaInfo.value.progress)
    }
})

</script>

<template>
    <div class="media-container">
        <video v-if="src" id="media" controls>
            <source :src="src!">
            Your browser does not support the video tag.
        </video>
    </div>
</template>

<style scoped>
    .media-container{
        min-height: 1px;
        flex: 1 0 0;
        position: relative;
        overflow: hidden;;
    }

    #media {
        width: 100%;
        height: 100%;
        object-fit: contain; /* Maintains aspect ratio while covering the container */
        object-position: center; /* Horizontally centers the video and aligns to the top */
    }

</style>
