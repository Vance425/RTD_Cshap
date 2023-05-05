<script setup>
import { Nav, Alert, Footer } from '@/components';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import '@/assets/js/flowbite.js'
const authStore = useAuthStore();
const { user } = storeToRefs(authStore);
</script>

<template>
    <div class="bg-white dark:bg-gray-900">
        <Nav />
        <Alert />
        <router-view />
        <Footer />
    </div>
</template>

<style>
    @import "tabulator-tables";
    @import "@/assets/css/table.css";
</style>