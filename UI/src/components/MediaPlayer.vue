<script setup lang="ts">
import { pathService } from '@/services/PathService';
import { ref, watch } from 'vue';
const baseUrl = import.meta.env.VITE_BASE_URL;

var src = ref<string | null>(null)

watch(pathService.getWatcherPathRef(),(newVal, oldVal) => {
    updateSrc();
});

function updateSrc(){
    src.value = baseUrl + "/data/" + pathService.getPathString();
}
updateSrc();

</script>

<template>
    <video v-if="src" id="myVideo" controls>
        <source :src="src!">
        Your browser does not support the video tag.
    </video>
</template>
