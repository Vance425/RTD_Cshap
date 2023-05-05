<script setup>
import { onMounted } from 'vue';
import { Form, Field } from 'vee-validate';
import * as Yup from 'yup';
import { useAuthStore } from '@/stores';
import { currentTheme, initTheme, switchTheme } from '@/composables/theme.js'



const schema = Yup.object().shape({
    username: Yup.string().required('Username is required'),
    password: Yup.string().required('Password is required')
});

async function onSubmit(values) {
    const authStore = useAuthStore();
    const { username, password } = values;
    await authStore.login(username, password);
}

onMounted(() => {
  initTheme();
});
</script>

<template>
    <div class="flex justify-center h-screen">         
        <div class="items-center max-w-md px-6 mx-auto lg:w-1/2">
            <div class="flex-1">
                <div class="mt-12 pt-12">
                    <h1 class="mb-5 text-7xl font-bold text-gray-700 dark:text-white">JCET</h1>
                    <h2 class="text-4xl font-bold text-gray-700 dark:text-white">RTD SYSTEM</h2>
                </div>
                <div class="mt-8">
                    <p class="mb-3 text-xl font-bold text-gray-500 dark:text-gray-300">Login</p>
                    
                    <div class="p-4 max-w-sm bg-white rounded-lg border border-gray-200 shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
                        <Form @submit="onSubmit" :validation-schema="schema" v-slot="{ errors, isSubmitting }" class="space-y-6" action="#">
                            <div>
                                <label class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">使用者名稱 User Name</label>
                                <Field name="username" type="text" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" :class="{ 'is-invalid': errors.username }" />
                                <div class="invalid-feedback">{{ errors.username }}</div>
                            </div>
                            <div>
                              <label class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-400">部門 Department</label>
                              <select id="department" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
                                <option selected>Choose a Department</option>
                                <option value="d1">Department 01</option>
                                <option value="d2">Department 02</option>
                                <option value="d3">Department 03</option>
                                <option value="d4">Department 04</option>
                              </select>
                            </div>
                            <div>
                                <label class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">密碼 Password</label>
                                <Field name="password" type="password" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" :class="{ 'is-invalid': errors.password }" />
                                <div class="invalid-feedback">{{ errors.password }}</div>
                            </div>
                            <div class="flex items-start">
                                <div class="flex items-start">
                                    <div class="flex items-center h-5">
                                        <input id="remember" type="checkbox" value="" class="w-4 h-4 bg-gray-50 rounded border border-gray-300 focus:ring-3 focus:ring-blue-300 dark:bg-gray-700 dark:border-gray-600 dark:focus:ring-blue-600 dark:ring-offset-gray-800" required="">
                                    </div>
                                    <label for="remember" class="ml-2 text-sm font-medium text-gray-900 dark:text-gray-300">記得我 Remember me</label>
                                </div>
                            </div>
                            <button type="submit" class="w-full text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-5 py-2.5 text-center dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">登入 Login</button>
                        </Form>
                        <div>
                      </div>
                    </div>
                </div>
            </div>
            <footer class="w-full p-4 mt-12 md:flex md:items-center md:justify-between md:p-6">
                <span class="text-sm text-gray-500 dark:text-gray-400">Copyright © 2022 <a href="https://www.gyro.com.tw/" class="hover:underline">Gyro Systems</a>. All Rights Reserved.
                </span>
            </footer>
        </div>
        <div class="hidden bg-cover lg:block lg:w-1/2" :style="{backgroundImage: 'url(../src/assets/img/login/side_img.png)'}">
            <div class="h-full px-20 pt-10 bg-gray-900 bg-opacity-0">
                <!-- <div>
                    <h2 class="text-4xl font-bold text-white">Brand</h2>
                    
                    <p class="max-w-xl mt-3 text-gray-300">Lorem ipsum dolor sit, amet consectetur adipisicing elit. In autem ipsa, nulla laboriosam dolores, repellendus perferendis libero suscipit nam temporibus molestiae</p>
                </div> -->
                <div class="mb-4 text-right">
                    <!-- <div class="form-group">
                        <router-link to="register" class="btn btn-link">Register</router-link>
                    </div> -->
                    <button
                      class="overflow-hidden p-2 mr-3 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-600 bg-gray-50 dark:bg-gray-700  rounded-lg text-sm"
                      @click="switchTheme()"
                    >
                      <transition
                        enter-active-class="transition duration-200 ease-out"
                        leave-active-class="transition duration-200 ease-in"
                        :enter-from-class="currentTheme === 'dark' ? 'transform -translate-y-full scale-50 opacity-0' : 'transform translate-y-full scale-50 opacity-0'"
                        enter-to-class="transform translate-y-0"
                        leave-from-class="transform translate-y-0"
                        :leave-to-class="currentTheme === 'dark' ? 'transform translate-y-full scale-50 opacity-0' : 'transform -translate-y-full scale-50 opacity-0'"
                        mode="out-in"
                      >
                        <svg
                          v-if="currentTheme === 'dark'"
                          xmlns="http://www.w3.org/2000/svg"
                          class="w-5 h-5"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                        >
                          <path d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                        </svg>

                        <svg
                          v-else
                          xmlns="http://www.w3.org/2000/svg"
                          class="w-5 h-5"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                          stroke-linecap="round"
                          stroke-linejoin="round"
                          stroke-width="2"
                        >
                          <path d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
                        </svg>
                      </transition>
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>