<template>
    <div class="bg-white dark:bg-gray-900">
        <div class="flex justify-center h-screen">         
            <div class="flex flex-wrap items-center w-full max-w-md px-6 mx-auto lg:w-1/2">
                <div class="flex-1">
                    <div class="mt-12 pt-12">
                        <h1 class="mb-5 text-7xl font-bold text-gray-700 dark:text-white">JCET</h1>
                        <h2 class="text-4xl font-bold text-gray-700 dark:text-white">RTD SYSTEM</h2>
                    </div>
                    <div class="mt-8">
                        <p class="mb-3 text-xl font-bold text-gray-500 dark:text-gray-300">Login</p>
                        
                        <div class="p-4 max-w-sm bg-white rounded-lg border border-gray-200 shadow-md sm:p-6 md:p-8 dark:bg-gray-800 dark:border-gray-700">
                            <form class="space-y-6" action="#">
                                <div>
                                    <label for="username" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">使用者名稱 User Name</label>
                                    <input type="username" name="username" id="username" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" placeholder="Please input username" required="">
                                </div>
                                <div>
                                  <label for="department" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-400">部門 Department</label>
                                  <select id="department" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
                                    <option selected>Choose a Department</option>
                                    <option value="d1">Department 01</option>
                                    <option value="d2">Department 02</option>
                                    <option value="d3">Department 03</option>
                                    <option value="d4">Department 04</option>
                                  </select>
                                </div>
                                <div>
                                    <label for="password" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">密碼 Password</label>
                                    <input type="password" name="password" id="password" placeholder="Please input password" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-600 dark:border-gray-500 dark:placeholder-gray-400 dark:text-white" required="">
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
                            </form>
                        </div>
                    </div>
                </div>
                <footer class="w-full p-4 mt-12 md:flex md:items-center md:justify-between md:p-6">
                    <span class="text-sm text-gray-500 dark:text-gray-400">Copyright © 2022 <a href="https://www.gyro.com.tw/" class="hover:underline">Gyro Systems</a>. All Rights Reserved.
                    </span>
                </footer>
            </div>
            <div class="hidden bg-cover lg:block lg:w-1/2" style="background-image: url('static/img/login/side_img.png')">
                <div class="h-full px-20 pt-10 bg-gray-900 bg-opacity-0">
                    <!-- <div>
                        <h2 class="text-4xl font-bold text-white">Brand</h2>
                        
                        <p class="max-w-xl mt-3 text-gray-300">Lorem ipsum dolor sit, amet consectetur adipisicing elit. In autem ipsa, nulla laboriosam dolores, repellendus perferendis libero suscipit nam temporibus molestiae</p>
                    </div> -->
                    <div class="mb-4 text-right">
                      <button id="theme-toggle" type="button" class="text-gray-900 bg-white hover:bg-gray-100 border border-gray-200 focus:ring-4 focus:outline-none focus:ring-gray-100 font-medium rounded-lg text-sm px-2.5 py-2.5 text-center inline-flex items-center dark:focus:ring-gray-600 dark:bg-gray-800 dark:border-gray-700 dark:text-white dark:hover:bg-gray-700 mr-2 mb-2">
                          <svg id="theme-toggle-dark-icon" class="hidden w-5 h-5" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M17.293 13.293A8 8 0 016.707 2.707a8.001 8.001 0 1010.586 10.586z"></path></svg>
                          <svg id="theme-toggle-light-icon" class="hidden w-5 h-5" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M10 2a1 1 0 011 1v1a1 1 0 11-2 0V3a1 1 0 011-1zm4 8a4 4 0 11-8 0 4 4 0 018 0zm-.464 4.95l.707.707a1 1 0 001.414-1.414l-.707-.707a1 1 0 00-1.414 1.414zm2.12-10.607a1 1 0 010 1.414l-.706.707a1 1 0 11-1.414-1.414l.707-.707a1 1 0 011.414 0zM17 11a1 1 0 100-2h-1a1 1 0 100 2h1zm-7 4a1 1 0 011 1v1a1 1 0 11-2 0v-1a1 1 0 011-1zM5.05 6.464A1 1 0 106.465 5.05l-.708-.707a1 1 0 00-1.414 1.414l.707.707zm1.414 8.486l-.707.707a1 1 0 01-1.414-1.414l.707-.707a1 1 0 011.414 1.414zM4 11a1 1 0 100-2H3a1 1 0 000 2h1z" fill-rule="evenodd" clip-rule="evenodd"></path></svg>
                      </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>
<script>
import { onMounted, ref } from "vue";

export default {
  setup() {
    
      if (localStorage.getItem('color-theme') === 'dark' || (!('color-theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
          document.documentElement.classList.add('dark');
          
      } else {
          document.documentElement.classList.remove('dark')
      }

      function darkMode() {
        const themeToggleDarkIcon = document.getElementById('theme-toggle-dark-icon');
          const themeToggleLightIcon = document.getElementById('theme-toggle-light-icon');

          // Change the icons inside the button based on previous settings
          if (localStorage.getItem('color-theme') === 'dark' || (!('color-theme' in localStorage) && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
              themeToggleLightIcon.classList.remove('hidden');
          } else {
              themeToggleDarkIcon.classList.remove('hidden');
          }

          const themeToggleBtn = document.getElementById('theme-toggle');

          themeToggleBtn.addEventListener('click', function() {

              // toggle icons inside button
              themeToggleDarkIcon.classList.toggle('hidden');
              themeToggleLightIcon.classList.toggle('hidden');

              // if set via local storage previously
              if (localStorage.getItem('color-theme')) {
                  if (localStorage.getItem('color-theme') === 'light') {
                      document.documentElement.classList.add('dark');
                      localStorage.setItem('color-theme', 'dark');
                  } else {
                      document.documentElement.classList.remove('dark');
                      localStorage.setItem('color-theme', 'light');
                  }

              // if NOT set via local storage previously
              } else {
                  if (document.documentElement.classList.contains('dark')) {
                      document.documentElement.classList.remove('dark');
                      localStorage.setItem('color-theme', 'light');
                  } else {
                      document.documentElement.classList.add('dark');
                      localStorage.setItem('color-theme', 'dark');
                  }
              }
              
          });
      }

      onMounted(() => {
        darkMode()
      })
  },


}
</script>
<style scoped>

</style>
