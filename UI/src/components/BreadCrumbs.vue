<script setup lang="ts">

import { pathService } from '@/services/PathService';
import { watch, shallowRef, type ShallowRef, ref } from 'vue';


var crumbs = ref<string[]>([]);
watch(pathService.getPath(), update);
update();
function update() {
    crumbs.value = ["root", ...pathService.getPathString().split("/").filter(z => !!z)]
}

function navigate(index: number) {
    pathService.navigate(index);
}

</script>

<template>
    <nav aria-label="breadcrumb" style="padding-left: 8px;">
        <ol class="breadcrumb">
            <li v-for="(item, index) in crumbs" :key="index" class="breadcrumb-item">
                <!-- If it's not the last item, make it a clickable link -->
                <template v-if="index < crumbs.length - 1">
                    <a class="link" href="#" @click.prevent="navigate(index)">{{ item }}</a>
                </template>
                <!-- If it's the last item, just display text -->
                <template v-else>
                    <span v-if="index != 0">&nbsp;</span>{{ item }}
                </template>
            </li>
        </ol>
    </nav>
</template>

<style scoped>

</style>
