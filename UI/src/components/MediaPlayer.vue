<script setup lang="ts">
import { pathService } from '@/services/PathService';
import { mediaService } from '@/services/MediaService';
import { onMounted, ref, shallowRef, watch } from 'vue';
import type { MediaInfo } from '@/models/models';
import { apiAccess } from '@/services/ApiAccess';
import BreadCrumbs from './BreadCrumbs.vue';
const baseUrl = import.meta.env.VITE_BASE_URL;

var src = ref<string | null>(null)
var mediaInfo = shallowRef<MediaInfo | null>(pathService.getMediaInfo());
var mounted = false;
watch(pathService.getPath(),(newVal, oldVal) => {
    updateSrc();
});
function updateSrc(){
    //we can't encodeURIComponent the path string because the slashes need to be not encoded
    var path = pathService.getPath().value.map(z => encodeURIComponent(z));
    src.value = baseUrl + "/data/" + path.join("/");
}
updateSrc();
if (!mediaInfo.value){
    apiAccess.getMediaInfo(pathService.getPathString()).then(z => {
        mediaInfo.value = z
        if (mounted){
            document.title = mediaInfo.value.fileName;
            mediaService.setup(pathService.getPathString(), mediaInfo.value.progress)
        }
    });
}
onMounted(() => {
    mounted = true;
    if (mediaInfo.value){
        document.title = mediaInfo.value.fileName;
        mediaService.setup(pathService.getPathString(), mediaInfo.value.progress)
    }
})

</script>

<template>
    <BreadCrumbs></BreadCrumbs>
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
