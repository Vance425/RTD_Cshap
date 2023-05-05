<script>
import { onBeforeMount, onMounted, reactive, ref, watch } from 'vue'; 
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import "@/assets/js/flowbite.js"
import { AmrResource, CarrierResource, RtdTable } from '@/views/dashboard'
import Drawer from '@/components/Drawer.vue'
export default {
    setup() {
        const authStore = useAuthStore();
        const { user } = storeToRefs(authStore);
        
    },
    components:{
        AmrResource, CarrierResource, RtdTable, Drawer
    },
    data() {
        return {
            isVisible: true
        }
    },
    methods:{
        openMenu(){
            if(this.$refs.LeftDrawer.active){
                this.$refs.LeftDrawer.close();					
            }else{
                this.$refs.LeftDrawer.open();
            }
        },
    },
    
}

</script>

<template>
<main class="flex flex-row my-4 mx-4 gap-4 container-fluid h-screen">
    <aside class="flex flex-col w-1/5">
        <AmrResource />
        <CarrierResource />
    </aside>

    <article class="flex flex-col w-4/5">
        <div class="bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700 overflow-y-hidden">
            <RtdTable />
        </div>
    </article>
</main>
    <div v-if="user">
        <h1>Hi {{user.firstName}}!</h1>
        <p>You're logged in with Vue 3 + Pinia & JWT!!</p>
        <p><router-link to="/users">Manage Users</router-link></p>
    </div>
    <div class="text-white dark:text-gray-50">
        <!-- {{ resData }} -->
    </div>
</template>
<style>

</style>