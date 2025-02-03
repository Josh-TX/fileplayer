<script setup lang="ts">
import { ref } from 'vue';
import FolderList from './components/FolderList.vue';
import { pathService } from '@/services/PathService';
import BreadCrumbs from './components/BreadCrumbs.vue';
import MediaPlayer from './components/MediaPlayer.vue';
import FolderSelectModal from './components/FolderSelectModal.vue'

</script>

<template>
    <div style="display: flex; flex-direction: column; height: 100%;">
        <BreadCrumbs></BreadCrumbs>
        <FolderList v-if="!pathService.isFile().value"></FolderList>
        <MediaPlayer v-if="pathService.isFile().value"></MediaPlayer>
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
