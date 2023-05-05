<template>
<!-- drawer component -->
        <div id="drawer-dataform" class="fixed z-40 h-screen p-4 overflow-y-auto bg-white w-full dark:bg-gray-800" tabindex="-1" aria-labelledby="drawer-dataform-label">
        <h5 id="drawer-label" class="inline-flex items-center mt-2 mb-6 text-2xl font-semibold text-gray-500 uppercase dark:text-gray-400">LOT Control</h5>
        <button type="button" data-drawer-dismiss="drawer-dataform" aria-controls="drawer-dataform" class="text-gray-400 bg-transparent hover:bg-gray-200 hover:text-gray-900 rounded-lg text-sm p-1.5 absolute top-2.5 right-2.5 inline-flex items-center dark:hover:bg-gray-600 dark:hover:text-white" @click="openMenu">
            <svg aria-hidden="true" class="w-10 h-10" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path></svg>
            <span class="sr-only">Close menu</span>
        </button>
            <form action="#" class="mb-6">
                    <div class="p-4 mb-4 w-full bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
                        <div class="flex justify-between items-center mb-4">
                            <h5 class="text-xl font-bold leading-none text-gray-900 dark:text-gray-400">Status</h5>
                        </div>
                        <div class="flow-root">
                            <div class="p-4 mt-4 w-full bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                <div class="flex flex-row">
                                    <div class="mb-6 flex-col w-1/2 pr-5">
                                        <label for="email" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">LOT ID</label>
                                        <input type="email" id="email" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="" required>
                                    </div>
                                    <div class="mb-6 flex-col w-1/2 pr-5">
                                        <label for="email" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">Status</label>
                                        <input type="email" id="email" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="" required>
                                    </div>
                                </div>
                                <div class="flex flex-row">
                                    <div class="mb-6 flex-col w-1/2 pr-5">
                                        <label for="email" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">PRIORITY</label>
                                        <input type="email" id="email" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="" required>
                                    </div>
                                </div>
                            </div>
                            <hr class="my-3 border-slate-400 dark:border-slate-600">
                            <div class="p-4 w-full max-w-md bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                <div class="flow-root">

                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="p-4 mb-4 w-full bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
                        <div class="flex justify-between items-center mb-4">
                            <h5 class="text-xl font-bold leading-none text-gray-900 dark:text-gray-400">ACTIONS</h5>
                        </div>
                        <div class="flow-root">
                            <div class="p-4 mt-4 w-full bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                <div class="flex flex-row">
                                    <div class="flex-col w-1/2 pr-5">
                                        <button type="submit" class="text-white bg-teal-700 hover:bg-teal-800 w-full focus:ring-4 focus:ring-teal-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-teal-600 dark:hover:bg-teal-700 focus:outline-none dark:focus:ring-teal-800 block">HOLD</button>
                                    </div>
                                    <div class="flex-col w-1/2">
                                        <button type="submit" class="text-white bg-teal-700 hover:bg-teal-800 w-full focus:ring-4 focus:ring-teal-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-teal-600 dark:hover:bg-teal-700 focus:outline-none dark:focus:ring-teal-800 block">DISPATCH</button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
            </form>
        </div>
</template>
<script>

</script>
<style scoped>

</style>