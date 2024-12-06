<script setup lang="ts">

import { pathService } from '@/services/PathService';
import { watch, shallowRef, type ShallowRef, ref } from 'vue';


var crumbs = ref<string[]>([]);
watch(pathService.getWatcherPathRef(), update);
update();
function update(){
    crumbs.value = ["/", ...pathService.getPathString().split("/").filter(z => !!z)]
}

function navigate(index: number){
    pathService.navigate(index);
}

</script>

<template>
    <nav aria-label="breadcrumb">
        <ol class="breadcrumb">
            <li v-for="(item, index) in crumbs" :key="index" class="breadcrumb-item">
                <!-- If it's not the last item, make it a clickable link -->
                <template v-if="index < crumbs.length - 1">
                    <a href="#" @click.prevent="navigate(index)">{{ item }}</a>
                </template>
                <!-- If it's the last item, just display text -->
                <template v-else>
                    {{ item }}
                </template>
            </li>
        </ol>
    </nav>
</template>

<style scoped>
.breadcrumb {
  padding: 0;
  margin: 0;
  list-style: none;
  display: flex;
}

.breadcrumb-item {
  margin-right: 5px;
}

.breadcrumb-item a {
  text-decoration: none;
}

.breadcrumb-item::after {
  content: '>';
  margin-left: 5px;
}

.breadcrumb-item:last-child::after {
  content: '';
}
</style>
