<script setup lang="ts">

import { pathService } from '@/services/PathService';
import { watch, shallowRef, type ShallowRef, ref } from 'vue';


var crumbs = ref<string[]>([]);
watch(pathService.getPath(), update);
update();
function update() {
    crumbs.value = ["root", ...pathService.getPathString().split("/").filter(z => !!z)]
    setTimeout(() => {
        var el = document.getElementById("main-breadcrumb")!;
        el.scrollLeft = el.scrollWidth - el.clientWidth;
    })
}

function navigate(index: number) {
    pathService.navigate(index);
}

</script>

<template>
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb" id="main-breadcrumb">
            <li v-for="(item, index) in crumbs" :key="index" class="breadcrumb-item">
                <!-- If it's not the last item, make it a clickable link -->
                <template v-if="index < crumbs.length - 1">
                    <a class="link" href="#" @click.prevent="navigate(index)"><img v-if="index == 0" src="/favicon-32x32.png" class="text-logo">{{ item }}</a>
                </template>
                <!-- If it's the last item, just display text -->
                <template v-else>
                    <span v-if="index != 0">&nbsp;</span><img v-if="index == 0" src="/favicon-32x32.png" class="text-logo">{{ item }}
                </template>
            </li>
        </ol>
    </nav>
</template>

<style scoped>
.text-logo{
    height: 1em; 
    vertical-align: middle;
    position: relative;
    top: -1px;
    padding-right: 4px;
}
</style>
