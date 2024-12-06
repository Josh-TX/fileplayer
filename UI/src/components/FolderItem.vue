<script setup lang="ts">
defineProps<{
  name: string
}>()
</script>

<template>
  <div class="folder-container">
    <h1 class="green">{{ name }}</h1>
  </div>
</template>
