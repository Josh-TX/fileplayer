<template>
    <div class="dropdown" :class="{ 'is-active': isActive }">
      <button class="dropdown-trigger" @click="toggleDropdown">
        {{ text }}
        <span class="caret">&#9662;</span>
      </button>
      <div class="dropdown-menu">
        <ul class="dropdown-content">
          <li v-for="(item, index) in items" :key="index" @click="handleClick(item)">
            {{ item }}
          </li>
        </ul>
      </div>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref } from 'vue';
  
  // Props
  const props = defineProps({
    text: {
      type: String,
      default: 'Dropdown',
    },
    items: {
      type: Array,
      default: () => [],
    },
  });
  
  // State
  const isActive = ref(false);
  
  // Methods
  function toggleDropdown() {
    isActive.value = !isActive.value;
  }
  
  function handleClick(item) {
    console.log('Clicked item:', item);
    isActive.value = false; // Close dropdown after clicking
  }
  </script>
  
  <style scoped>
  .dropdown {
    position: relative;
    display: inline-block;
  }
  
  .dropdown-trigger {
    background-color: #f8f9fa;
    border: 1px solid #ccc;
    padding: 8px 12px;
    border-radius: 4px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }
  
  .caret {
    margin-left: 8px;
  }
  
  .dropdown-menu {
    position: absolute;
    top: 100%;
    left: 0;
    background: white;
    border: 1px solid #ccc;
    border-radius: 4px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    margin-top: 4px;
    display: none;
    z-index: 1000;
  }
  
  .dropdown.is-active .dropdown-menu {
    display: block;
  }
  
  .dropdown-content {
    list-style: none;
    margin: 0;
    padding: 0;
  }
  
  .dropdown-content li {
    padding: 8px 12px;
    cursor: pointer;
    transition: background-color 0.2s;
  }
  
  .dropdown-content li:hover {
    background-color: #f1f1f1;
  }
  </style>
  