<script>
  import { onBeforeMount, onMounted, reactive, ref, watch, getCurrentInstance, inject } from 'vue'; 
  import { storeToRefs } from 'pinia';
  import { useAuthStore } from '@/stores';
  import CommonMixin from '@/utils/CommonMixin'
  import { TabulatorFull as Tabulator } from 'tabulator-tables';
  import { useToast } from "vue-toastification";
import axios from 'axios';
  
  export default {
      setup(props, cxt) {
          const currentInstance = getCurrentInstance()
          const { $http, $message, $route, $axios } = currentInstance.appContext.config.globalProperties
          const toast = useToast();
          const { getData } = CommonMixin();
          const cmdListRef = ref();
          const cmdListTabulator = ref(); 
          const cmdListTableData = reactive([]);

          const allCmdList = ref()
  
          let modalSel = reactive()
          let editData = reactive({})
          let priorityEditOpen = ref(false)
          
  
      const initCmdList = () => {
        console.log("CMD",cmdListTableData.value)
          cmdListTabulator.value = new Tabulator(cmdListRef.value, {
              placeholder:"No Data Available", 
              data:cmdListTableData.value, 
              autoResize:false,
              pagination: "remote",
              paginationSize: 10,
              rowHeight: 55,
              paginationSizeSelector: [10,25,50,100],
              paginationCounter: "rows",
              layout:"fitColumns",
              reactiveData:true, 
              selectable: 1,
              height:690,
              columns:[
                  { title: "UUID", field: "UUID", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "CMD_ID", field: "CMD_ID", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "PRIORITY", field: "PRIORITY", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "CMD_TYPE", field: "CMD_TYPE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "EQUIPID", field: "EQUIPID", headerHozAlign:"center", hozAlign: "center", width: 180},
                  // { title: "CMD_STATE", field: "CMD_STATE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "CMD_CURRENT_STATE", field: "CMD_CURRENT_STATE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "CARRIERID", field: "CARRIERID", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "SOURCE", field: "SOURCE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "DEST", field: "DEST", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "CARRIERTYPE", field: "CARRIERTYPE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "REPLACE", field: "REPLACE", headerHozAlign:"center", hozAlign: "center", width: 180},
                  // { title: "BACK", field: "BACK", headerHozAlign:"center", hozAlign: "center", width: 180},
                  // { title: "ISLOCK", field: "ISLOCK", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "INITIAL_DT", field: "INITIAL_DT", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "WAITINGQUEUE_DT", field: "WAITINGQUEUE_DT", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "EXECUTEQUEUE_DT", field: "EXECUTEQUEUE_DT", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "COMPLETE_DT", field: "COMPLETE_DT", headerHozAlign:"center", hozAlign: "center", width: 180},
                  { title: "LASTMODIFY_DT", field: "LASTMODIFY_DT", headerHozAlign:"center", hozAlign: "center", width: 180},
              ],
          })
      }
    
      function closeDrawer(){
          modalSel.hide()
      }
      
      function refreshTableData() {
          console.log("DATA REFRESH")
          this.tabulator.refreshData()
          console.log("DATA REFRESH 2")
      }
  
  
      
      onMounted(() => {
          // set the modal menu element
          const targetEl = document.getElementById('drawer-dataform');
  
          const options = {
          placement: 'right',
          backdrop: true,
          bodyScrolling: false,
          edge: false,
          edgeOffset: '',
          backdropClasses: 'bg-gray-900 bg-opacity-50 dark:bg-opacity-80 fixed inset-0 z-30',
          onHide: () => {
              console.log('drawer is hidden');
          },
          onShow: () => {
              console.log('drawer is shown');
          },
          onToggle: () => {
              console.log('drawer has been toggled');
          }
          };
  
          modalSel = new Drawer(targetEl, options);
          
      })
      
      watch(editData, (newVal)=>{
          console.log("reactive:", newVal)
      })
  
      watch(priorityEditOpen, (newVal)=>{
          console.log("ref:", newVal)
      })
  
  
      return { editData, priorityEditOpen, closeDrawer, toast, cmdListTabulator, initCmdList, refreshTableData, allCmdList, cmdListTableData, cmdListRef }

      },
      data() {
        return {
          allList: '',
        };
      },
      methods: {
        getAllCmdList() {
          
          const api = 'http://192.168.0.88:5001/GetUIData/GetWorkInProcessSch';
          this.$http.get(api)
            .then((res) => {

              if (res.statusText == "OK") {
                console.log(res, '取得CMD資料');
                // this.cmdListTableData.value = [{"UUID":"U2022090606240000182","CMD_ID":"C20220906062400362","CMD_TYPE":"TRANS","EQUIPID":"3PBG1-D","CMD_STATE":"Failed","CMD_CURRENT_STATE":"Failed","CARRIERID":"12-F02","CARRIERTYPE":"A12","SOURCE":"*","DEST":"3PBG1-D-LP03","PRIORITY":10.0,"REPLACE":3.0,"BACK":"*","INITIAL_DT":null,"WAITINGQUEUE_DT":null,"EXECUTEQUEUE_DT":null,"COMPLITE_DT":null,"LASTMODIFY_DT":"2022-09-06T07:11:37","ISLOCK":0.0}]
                this.cmdListTableData.value = res.data
                console.log('取得CMD資料AA', this.cmdListTableData.value );
              }
              this.initCmdList()
            })
            .catch((error) => {
              console.log(error, '連線錯誤');
            });
        },
      },
      created () {
            this.getAllCmdList()
      },
  }
  
  </script>
  
  <template>
          <div ref="cmdDom" class="p-4 bg-white rounded-lg md:p-8 dark:bg-gray-800" id="command" role="tabpanel" aria-labelledby="command-tab">
              <h2 class="mb-5 text-2xl font-extrabold tracking-tight text-gray-900 dark:text-white">Current：  ORDER</h2>
              <div id="cmdListTabulator" ref="cmdListRef" class="text-sm text-left text-gray-500 dark:text-gray-400 pb-3"></div>
          </div>
  </template>