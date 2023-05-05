<script setup>
import { onBeforeMount, onMounted, reactive, ref, watch, getCurrentInstance, inject } from 'vue'; 
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import { LotListTab, OrderListTab, CarrierListTab, EquipmentListTab } from '@/components'


</script>

<template>
    <ul class="hidden text-xl font-medium text-center text-gray-500 rounded-lg divide-x divide-gray-200 sm:flex dark:divide-gray-600 dark:text-gray-400" id="fullWidthTab" data-tabs-toggle="#fullWidthTabContent" role="tablist">
        <li class="w-full">
            <button id="lot-tab" data-tabs-target="#lot" type="button" role="tab" aria-controls="lot" aria-selected="true" class="inline-block p-4 w-full  bg-gray-50 rounded-tl-lg hover:bg-gray-100 focus:outline-none dark:bg-gray-700 dark:hover:bg-gray-600 dark:border-transparent text-gray-500 hover:text-gray-600 dark:text-gray-400 border-gray-100 hover:border-gray-300 dark:border-gray-700 dark:hover:text-gray-300" forceRender>Lot</button>
        </li>
        <li class="w-full">
            <button id="carrier-tab" data-tabs-target="#carrier" type="button" role="tab" aria-controls="carrier" aria-selected="false" class="inline-block p-4 w-full bg-gray-50 hover:bg-gray-100 focus:outline-none dark:bg-gray-700 dark:hover:bg-gray-600 dark:border-transparent text-gray-500 hover:text-gray-600 dark:text-gray-400 border-gray-100 hover:border-gray-300 dark:border-gray-700 dark:hover:text-gray-300" forceRender @click="this.$refs.carrier.getAllCarrierList()">Carrier</button>
        </li>
        <li class="w-full">
            <button id="equipment-tab" data-tabs-target="#equipment" type="button" role="tab" aria-controls="equipment" aria-selected="false" class="inline-block p-4 w-full bg-gray-50 hover:bg-gray-100 focus:outline-none dark:bg-gray-700 dark:hover:bg-gray-600 dark:border-transparent text-gray-500 hover:text-gray-600 dark:text-gray-400 border-gray-100 hover:border-gray-300 dark:border-gray-700 dark:hover:text-gray-300" forceRender @click="this.$refs.equipment.getAllEquipList()">Equipment</button>
        </li>
        <li class="w-full">
            <button id="command-tab" data-tabs-target="#command" type="button" role="tab" aria-controls="command" aria-selected="false" class="inline-block p-4 w-full bg-gray-50 rounded-tr-lg hover:bg-gray-100 focus:outline-none dark:bg-gray-700 dark:hover:bg-gray-600 dark:border-transparent text-gray-500 hover:text-gray-600 dark:text-gray-400 border-gray-100 hover:border-gray-300 dark:border-gray-700 dark:hover:text-gray-300" forceRender @click="this.$refs.order.getAllCmdList()">Order</button>
        </li>
    </ul>
    <div id="fullWidthTabContent" class="border-t border-gray-200 dark:border-gray-600">
        <LotListTab />
        <CarrierListTab ref="carrier" />
        <EquipmentListTab ref="equipment" />
        <OrderListTab ref="order" />
    </div>
</template>