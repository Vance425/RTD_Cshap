import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from './router';
import Axios from 'axios'
import VueAxios from 'vue-axios';
import Vue3EasyDataTable from 'vue3-easy-data-table';
import 'vue3-easy-data-table/dist/style.css';
import Toast from "vue-toastification";
import "vue-toastification/dist/index.css";

// setup fake backend
import { fakeBackend } from './helpers';
fakeBackend();
import jQuery from 'jquery';
import '@/assets/css/index.css'

const app = createApp(App);
app.config.globalProperties.$http = Axios
app.config.globalProperties.$axios = Axios
          app.use(createPinia());
          app.use(router);
          app.use(Toast,{
            transition: "Vue-Toastification__bounce",
            maxToasts: 20,
            newestOnTop: true
          });
          app.use(VueAxios, Axios)
          app.use(jQuery);
          app.component('EasyDataTable', Vue3EasyDataTable);
          app.mount('#app');
