<script setup>

</script>

<template>
    <div class="p-4 mb-4 w-full max-w-md bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
        <div class="flex justify-between items-center mb-4">
            <h5 class="text-3xl font-bold leading-none text-gray-900 dark:text-white">AMR Resource</h5>
        </div>
        <h5 class="text-lg font-bold leading-none text-gray-900 dark:text-white">Status</h5>
        <div class="flow-root">
            <div class="p-4 mt-4 w-full max-w-md bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                <div class="flow-root">
                    <ul role="list" class="divide-y divide-gray-400 dark:divide-gray-700">
                        <li class="pb-3">
                            <div class="flex items-center">
                                <div class="flex-shrink-0">

                                </div>
                                <div class="flex-1 min-w-0">
                                    <p class="text-2xl font-medium text-gray-900 truncate dark:text-white">
                                        IDLE
                                    </p>
                                    <p class="text-3xl text-green-700 font-bold text-center truncate dark:text-green-400">
                                        20
                                    </p>
                                </div>
                            </div>
                        </li>
                        <li class="pt-3">
                            <div class="flex items-center">
                                <div class="flex-shrink-0">

                                </div>
                                <div class="flex-1 min-w-0">
                                    <p class="text-2xl font-medium text-gray-900 truncate dark:text-white">
                                        BUSY
                                    </p>
                                    <p class="text-3xl text-red-700 font-bold text-center truncate dark:text-red-400">
                                        15
                                    </p>
                                </div>
                            </div>
                        </li>
                    </ul>
                </div>
            </div>
        </div>
    </div>  
</template>