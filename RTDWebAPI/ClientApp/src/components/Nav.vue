<script setup>
import { onBeforeMount, onMounted, ref, reactive, getCurrentInstance, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useAuthStore } from '@/stores';
import { currentTheme, initTheme, switchTheme } from '@/composables/theme.js'
import { useToast } from "vue-toastification";

const toast = useToast();
const authStore = useAuthStore();
const { user } = storeToRefs(authStore);
const currentInstance = getCurrentInstance()
let modeControl = reactive({ mode:'' })
const { $http, $message, $route, $axios } = currentInstance.appContext.config.globalProperties
console.log("DATA", user)



onBeforeMount(() => {
  function getControlMode() {
        $http.get('http://192.168.0.88:5001/GetUIData/GetExecuteMode')
          .then((res) => {
              //  console.log("GET MODE", res)
              if (res.statusText == "OK") {
                  modeControl.mode = res.data[0]
                  console.log("GET MODE2", modeControl)
              }
          })
    }
    getControlMode()
});

watch(modeControl, (newVal)=>{
    console.log("MODE CHANGE:", newVal)
})

onMounted(() => {
  initTheme();
  
});

    function getFullscreenElement() {
        return document.fullscreenElement   //standard property
        || document.webkitFullscreenElement //safari/opera support
        || document.mozFullscreenElement    //firefox support
        || document.msFullscreenElement;    //ie/edge support
    }
  
    function toggleFullscreen() {
        if(getFullscreenElement()) {
          document.exitFullscreen();
        }else {
      document.documentElement.requestFullscreen().catch(console.log);
        }
    }

    function changeAutoMode() {
      let getuser = localStorage.getItem('user')
      let user = JSON.parse(getuser)
      console.log("USER", JSON.parse(getuser))

      let postMode = {
        "parameter": "string",
        "paramType": "string",
        "paramValue": "1",
        "modifyBy": user.username,
        "lastModify_DT": "2022-09-08T07:42:21.439Z",
        "description": "no description"
      }

      console.log("CHANGE AUTO MODE", postMode)
      
      $http.post('http://192.168.0.88:5001/GetUIData/ChangeExecuteMode', postMode)
      .then((res) => {
          //  console.log("GET MODE", res)
          if (res.statusText == "OK") {
              modeControl.mode = 1
              console.log("GET MODE2", modeControl)
              toast.success("AUTO MODE CHANGE", {
                  position: "bottom-right",
                  timeout: 2000,
              });
          }
      })
    }

    function changeSemiMode() {
        let getuser = localStorage.getItem('user')
        let user = JSON.parse(getuser)
        console.log("USER", JSON.parse(getuser))

        let postMode = {
          "parameter": "string",
          "paramType": "string",
          "paramValue": "0",
          "modifyBy": user.username,
          "lastModify_DT": "2022-09-08T07:42:21.439Z",
          "description": "no description"
        }

        console.log("CHANGE AUTO MODE", postMode)
        
        $http.post('http://192.168.0.88:5001/GetUIData/ChangeExecuteMode', postMode)
        .then((res) => {
            //  console.log("GET MODE", res)
            if (res.statusText == "OK") {
                modeControl.mode = 0
                console.log("GET MODE2", modeControl)

                toast.success("SEMI MODE CHANGE", {
                    position: "bottom-right",
                    timeout: 2000,
                });
            }
        })
    }

</script>

<template>
  <nav v-if="authStore.user" class="bg-white border-gray-200 px-2 md:px-4 py-2.5 rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
      <div class="flex flex-wrap justify-between items-center mx-auto px-3">
          <a href="/" class="flex items-center">
            <svg 
            v-if="currentTheme === 'dark'"
            xmlns="http://www.w3.org/2000/svg"
            class="mr-3 h-6 text-gary-700 dark:text-white"
            fill="currentColor"
            viewBox="0 0 155.91 42.52"
            >
              <path d="M28.91 2.62v28.93h-18v-5.71h-8.7v9.81s.5 4.47 4.47 4.47H33.5s4.84.12 4.84-4.84V2.62h-9.43zM76.84 2.62v8.32H53.62v21.23h27.81v7.95H50.88s-7.51.37-7.51-7.51v-21.3s.13-8.91 8.91-8.91 24.56.22 24.56.22zM91.24 10.94v6.71h26.45v7.95H91.24v6.46h26.45v8.07H91.24v-8.07h-9.81V10.94s.25-8.2 8.07-8.2h64.2v8.2h-13.04v29.18h-9.93V10.94H91.24z"/>
            </svg>
            <img v-else src="@/assets/img/jcet_logo_light.svg" class="mr-3 h-6 " alt="RTD Logo">
              <span class="self-center text-xl font-semibold whitespace-nowrap dark:text-white">RTD SYSTEMS</span>
          </a>
          <div class="flex items-center md:order-2">
              <button v-if="modeControl.mode == 1" type="button" class="text-white hover:bg-gradient-to-bl  rounded-lg text-sm px-5 py-2.5 text-center mr-2 font-bold hover:ring-2 hover:outline-none hover:ring-cyan-400 dark:hover:ring-cyan-200 transition-all ease-in duration-75 control-auto" @click="changeSemiMode">
                MODE：AUTO
              </button>
              <button v-if="modeControl.mode == 0" type="button" class="text-gray-700 hover:bg-gradient-to-bl  rounded-lg text-sm px-5 py-2.5 text-center mr-2 font-black hover:ring-2 hover:outline-none hover:ring-yellow-400 dark:hover:ring-yellow-200 transition-all ease-in duration-75 control-semi" @click="changeAutoMode">
                MODE：SEMI-AUTO
              </button>
              <span class="mx-2 text-gray-500">|</span>
              <button
                class="overflow-hidden p-2 mr-3 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm"
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
              <button
                class="overflow-hidden p-2 mr-3 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm"
                @click="toggleFullscreen()"
              >
                  <transition
                    enter-active-class="transition duration-400 ease-out"
                    leave-active-class="transition duration-400 ease-in"
                    :enter-from-class="currentTheme === 'dark' ? 'transform -translate-y-full scale-50 opacity-0' : 'transform translate-y-full scale-50 opacity-0'"
                    enter-to-class="transform translate-y-0"
                    leave-from-class="transform translate-y-0"
                    :leave-to-class="currentTheme === 'dark' ? 'transform translate-y-full scale-50 opacity-0' : 'transform -translate-y-full scale-50 opacity-0'"
                    mode="out-in"
                  >
                  <svg v-if="currentTheme === 'dark'"
                    xmlns="http://www.w3.org/2000/svg"
                    class="w-5 h-5"
                    fill="currentColor"
                    viewBox="0 0 24 24"
                    stroke="none"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2">
                    <path d="M2.667 4A1.333 1.333 0 0 1 4 2.667h2.667a1.333 1.333 0 0 0 0-2.667H4a4 4 0 0 0-4 4v2.667a1.333 1.333 0 0 0 2.667 0V4Zm0 16A1.333 1.333 0 0 0 4 21.333h2.667a1.333 1.333 0 0 1 0 2.667H4a4 4 0 0 1-4-4v-2.667a1.333 1.333 0 0 1 2.667 0V20ZM20 2.667A1.333 1.333 0 0 1 21.333 4v2.667a1.333 1.333 0 0 0 2.667 0V4a4 4 0 0 0-4-4h-2.667a1.333 1.333 0 0 0 0 2.667H20ZM21.333 20A1.333 1.333 0 0 1 20 21.333h-2.667a1.333 1.333 0 0 0 0 2.667H20a4 4 0 0 0 4-4v-2.667a1.334 1.334 0 0 0-2.667 0V20Z"/>
                  </svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    class="w-5 h-5"
                    fill="currentColor"
                    viewBox="0 0 24 24"
                    stroke="none"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                  >
                    <path d="M2.667 4A1.333 1.333 0 0 1 4 2.667h2.667a1.333 1.333 0 0 0 0-2.667H4a4 4 0 0 0-4 4v2.667a1.333 1.333 0 0 0 2.667 0V4Zm0 16A1.333 1.333 0 0 0 4 21.333h2.667a1.333 1.333 0 0 1 0 2.667H4a4 4 0 0 1-4-4v-2.667a1.333 1.333 0 0 1 2.667 0V20ZM20 2.667A1.333 1.333 0 0 1 21.333 4v2.667a1.333 1.333 0 0 0 2.667 0V4a4 4 0 0 0-4-4h-2.667a1.333 1.333 0 0 0 0 2.667H20ZM21.333 20A1.333 1.333 0 0 1 20 21.333h-2.667a1.333 1.333 0 0 0 0 2.667H20a4 4 0 0 0 4-4v-2.667a1.334 1.334 0 0 0-2.667 0V20Z"/>
                  </svg>
                  </transition>
              </button>
              <button id="dropdownNotificationButton" data-dropdown-toggle="dropdownNotification" class="inline-flex items-center overflow-hidden py-2 pl-2 mr-3  text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg text-sm" type="button"> 
                <transition
                    enter-active-class="transition duration-800 ease-out"
                    leave-active-class="transition duration-800 ease-in"
                    :enter-from-class="currentTheme === 'dark' ? 'transform -translate-y-full scale-50 opacity-0' : 'transform translate-y-full scale-50 opacity-0'"
                    enter-to-class="transform translate-y-0"
                    leave-from-class="transform translate-y-0"
                    :leave-to-class="currentTheme === 'dark' ? 'transform translate-y-full scale-50 opacity-0' : 'transform -translate-y-full scale-50 opacity-0'"
                    mode="out-in"
                  >
                  <svg v-if="currentTheme === 'dark'"
                    xmlns="http://www.w3.org/2000/svg"
                    class="w-6 h-6"
                    fill="none"
                    viewBox="0 0 20 20"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"><path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6zM10 18a3 3 0 01-3-3h6a3 3 0 01-3 3z"></path></svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    class="w-6 h-6"
                    fill="none"
                    viewBox="0 0 20 20"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                  >
                    <path d="M10 2a6 6 0 00-6 6v3.586l-.707.707A1 1 0 004 14h12a1 1 0 00.707-1.707L16 11.586V8a6 6 0 00-6-6zM10 18a3 3 0 01-3-3h6a3 3 0 01-3 3z"></path>
                  </svg>
                
              </transition>
              <div class="flex relative">
                <div class="inline-flex relative -top-2 right-3 w-3 h-3 bg-green-500 rounded-full border-2 border-white dark:border-gray-900"></div>
              </div>
              </button>
              <!-- Dropdown menu -->
              <div id="dropdownNotification" class="hidden z-20 w-full max-w-sm bg-white rounded divide-y divide-gray-100 shadow dark:bg-gray-800 dark:divide-gray-700" aria-labelledby="dropdownNotificationButton">
                <div class="block py-2 px-4 font-medium text-center text-gray-700 bg-gray-50 dark:bg-gray-800 dark:text-white">
                    Notifications
                </div>
                <div class="divide-y divide-gray-100 dark:divide-gray-700">
                  <a href="#" class="flex py-3 px-4 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <div class="flex-shrink-0">
                      <div class="flex absolute justify-center items-center ml-6 -mt-5 w-5 h-5 bg-blue-600 rounded-full border border-white dark:border-gray-800">
                        <svg class="w-3 h-3 text-white" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M8.707 7.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l2-2a1 1 0 00-1.414-1.414L11 7.586V3a1 1 0 10-2 0v4.586l-.293-.293z"></path><path d="M3 5a2 2 0 012-2h1a1 1 0 010 2H5v7h2l1 2h4l1-2h2V5h-1a1 1 0 110-2h1a2 2 0 012 2v10a2 2 0 01-2 2H5a2 2 0 01-2-2V5z"></path></svg>
                      </div>
                    </div>
                    <div class="pl-3 w-full">
                        <div class="text-gray-500 text-sm mb-1.5 dark:text-gray-400"> Notification one </div>
                        <div class="text-xs text-blue-600 dark:text-blue-500">a few moments ago</div>
                    </div>
                  </a>
                  <a href="#" class="flex py-3 px-4 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <div class="flex-shrink-0">
                      <div class="flex absolute justify-center items-center ml-6 -mt-5 w-5 h-5 bg-gray-900 rounded-full border border-white dark:border-gray-800">
                        <svg class="w-3 h-3 text-white" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M8 9a3 3 0 100-6 3 3 0 000 6zM8 11a6 6 0 016 6H2a6 6 0 016-6zM16 7a1 1 0 10-2 0v1h-1a1 1 0 100 2h1v1a1 1 0 102 0v-1h1a1 1 0 100-2h-1V7z"></path></svg>
                      </div>
                    </div>
                    <div class="pl-3 w-full">
                        <div class="text-gray-500 text-sm mb-1.5 dark:text-gray-400"> Notification Two </div>
                        <div class="text-xs text-blue-600 dark:text-blue-500">10 minutes ago</div>
                    </div>
                  </a>
                  <a href="#" class="flex py-3 px-4 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <div class="flex-shrink-0">
                      <div class="flex absolute justify-center items-center ml-6 -mt-5 w-5 h-5 bg-red-600 rounded-full border border-white dark:border-gray-800">
                        <svg class="w-3 h-3 text-white" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" d="M3.172 5.172a4 4 0 015.656 0L10 6.343l1.172-1.171a4 4 0 115.656 5.656L10 17.657l-6.828-6.829a4 4 0 010-5.656z" clip-rule="evenodd"></path></svg>
                      </div>
                    </div>
                    <div class="pl-3 w-full">
                        <div class="text-gray-500 text-sm mb-1.5 dark:text-gray-400"> Notification three </div>
                        <div class="text-xs text-blue-600 dark:text-blue-500">44 minutes ago</div>
                    </div>
                  </a>
                  <a href="#" class="flex py-3 px-4 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <div class="flex-shrink-0">
                      <div class="flex absolute justify-center items-center ml-6 -mt-5 w-5 h-5 bg-green-400 rounded-full border border-white dark:border-gray-800">
                        <svg class="w-3 h-3 text-white" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" d="M18 13V5a2 2 0 00-2-2H4a2 2 0 00-2 2v8a2 2 0 002 2h3l3 3 3-3h3a2 2 0 002-2zM5 7a1 1 0 011-1h8a1 1 0 110 2H6a1 1 0 01-1-1zm1 3a1 1 0 100 2h3a1 1 0 100-2H6z" clip-rule="evenodd"></path></svg>
                      </div>
                    </div>
                    <div class="pl-3 w-full">
                        <div class="text-gray-500 text-sm mb-1.5 dark:text-gray-400"> Notification four </div>
                        <div class="text-xs text-blue-600 dark:text-blue-500">1 hour ago</div>
                    </div>
                  </a>
                  <a href="#" class="flex py-3 px-4 hover:bg-gray-100 dark:hover:bg-gray-700">
                    <div class="flex-shrink-0">
                      <div class="flex absolute justify-center items-center ml-6 -mt-5 w-5 h-5 bg-purple-500 rounded-full border border-white dark:border-gray-800">
                        <svg class="w-3 h-3 text-white" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M2 6a2 2 0 012-2h6a2 2 0 012 2v8a2 2 0 01-2 2H4a2 2 0 01-2-2V6zM14.553 7.106A1 1 0 0014 8v4a1 1 0 00.553.894l2 1A1 1 0 0018 13V7a1 1 0 00-1.447-.894l-2 1z"></path></svg>
                      </div>
                    </div>
                    <div class="pl-3 w-full">
                        <div class="text-gray-500 text-sm mb-1.5 dark:text-gray-400"> Notification five </div>
                        <div class="text-xs text-blue-600 dark:text-blue-500">3 hours ago</div>
                    </div>
                  </a>
                </div>
                <a href="#" class="block py-2 text-sm font-medium text-center text-gray-900 bg-gray-50 hover:bg-gray-100 dark:bg-gray-800 dark:hover:bg-gray-700 dark:text-white">
                  <div class="inline-flex items-center ">
                    <svg class="mr-2 w-4 h-4 text-gray-500 dark:text-gray-400" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z"></path><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"></path></svg>
                      View all
                  </div>
                </a>
              </div>

              <button type="button" class="flex mr-3 pr-3 text-sm dark:bg-gray-800 rounded-full md:mr-0 focus:ring-4 focus:ring-gray-300 dark:focus:ring-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700" id="user-menu-button" aria-expanded="false" data-dropdown-toggle="user-dropdown" data-dropdown-placement="bottom">
                <span class="sr-only">Open user menu</span>                
                <div class="flex items-center space-x-4">
                    <img class="w-10 h-10 rounded-full" src="@/assets/img/gyro_avatar.svg" alt="">
                    <div class="font-medium dark:text-white">
                        <div> {{ user.username }} </div>
                        <!-- <div class="text-sm text-gray-500 dark:text-gray-400"> {{ user.firstName  }} | {{ user.lastName }} </div> -->
                    </div>
                </div>
              </button>
              <!-- Dropdown menu -->
              <div class="hidden z-50 my-4 text-base list-none bg-white rounded divide-y divide-gray-100 shadow dark:bg-gray-700 dark:divide-gray-600" id="user-dropdown">
                <!-- <div class="py-3 px-4">
                  <span class="block text-sm font-medium text-gray-500 truncate dark:text-gray-400">  </span>
                </div> -->
                <ul class="py-1" aria-labelledby="user-menu-button">
                  <li>
                    <button @click="authStore.logout()" class="btn btn-link block py-2 px-4 text-sm text-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 dark:text-gray-200 dark:hover:text-white">Logout</button>
                  </li>
                </ul>
              </div>
              
          </div>
          <div id="mega-menu" class="hidden justify-between items-center w-full text-sm md:flex md:w-auto md:order-1">
              <ul class="flex flex-col mt-4 font-medium md:flex-row md:space-x-8 md:mt-0">
                  <!-- <li>
                      <button id="mega-menu-dropdown-button" data-dropdown-toggle="mega-menu-dropdown" type="button" class="flex flex-row items-center border-gary-500 text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 focus:outline-none focus:ring-4 focus:ring-gray-200 dark:focus:ring-gray-700 rounded-lg text-sm p-2.5">
                          <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z"></path></svg> <span class="flex ml-2">OPTIONS</span> 
                      </button>
                      <div id="mega-menu-dropdown" class="hidden grid absolute z-10 grid-cols-2 w-auto text-sm bg-white rounded-lg border border-gray-100 shadow-md dark:border-gray-700 md:grid-cols-3 dark:bg-gray-700 block" style="position: absolute; inset: 0px auto auto 0px; margin: 0px; transform: translate(134px, 70px);" data-popper-reference-hidden="" data-popper-escaped="" data-popper-placement="bottom">
                          <div class="p-4 pb-0 text-gray-900 md:pb-4 dark:text-white">
                              <ul class="space-y-4" aria-labelledby="mega-menu-dropdown-button">
                                  <li>
                                      <a href="#" class="text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-500">
                                          About Us
                                      </a>
                                  </li>
                                  <li>
                                      <a href="#" class="text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-500">
                                          Library
                                      </a>
                                  </li>
                                  <li>
                                      <a href="#" class="text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-500">
                                          Resources
                                      </a>
                                  </li>
                                  <li>
                                      <a href="#" class="text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-500">
                                          Pro Version
                                      </a>
                                  </li>
                              </ul>
                          </div>
                      </div>
                  </li> -->
              </ul>
          </div>
      </div>
  </nav>
</template>
<style>
  .control-auto.control-auto.control-auto.control-auto.control-auto {
    max-width: 200px;
    height: 40px;
    background: linear-gradient(-45deg, #03045e, #0077b6, #23a6d5, #23d5ab);
    background-size: 600% 600%;
    animation: gradientBG 5s ease infinite;
  }
  .control-semi.control-semi.control-semi.control-semi.control-semi {
    max-width: 200px;
    height: 40px;
    background: linear-gradient(-45deg, #ffbd00, #f9c80e, #ffe45e, #ff6d00);
    background-size: 600% 600%;
    animation: gradientBG 5s ease infinite;
  }

  @keyframes gradientBG {
    0% {
        background-position: 0% 50%;
    }
    50% {
        background-position: 100% 50%;
    }
    100% {
        background-position: 0% 50%;
    }
  }
</style>