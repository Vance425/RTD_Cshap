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
          const EquipListRef = ref();
          const EquipListTabulator = ref(); 
          const EquipListTableData = reactive([]);
  
          const allEquipList = ref()
  
          let modalSel = reactive()
          let editData = reactive({})
          let priorityEditOpen = ref(false)
          
  
      const initEquipList = () => {
              // console.log("Equip",EquipListTableData.value)
          EquipListTabulator.value = new Tabulator(EquipListRef.value, {
              placeholder:"No Data Available", 
              data:EquipListTableData.value, 
              autoResize:true,
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
                { title: "EQUIPID", field: "EQUIPID", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "EQUIP_DEPT", field: "EQUIP_DEPT", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "EQUIP_TYPE", field: "EQUIP_TYPE", headerHozAlign:"center", hozAlign: "center", width: 180 },
                // { title: "EQUIP_TYPEID", field: "EQUIP_TYPEID", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "MACHINE_STATE", field: "MACHINE_STATE", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "CURR_STATUS", field: "CURR_STATUS", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "DOWN_STATE", field: "DOWN_STATE", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "PORT_MODEL", field: "PORT_MODEL", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "PORT_NUMBER", field: "PORT_NUMBER", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "WORKGROUP", field: "WORKGROUP", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "NEAR_STOCKER", field: "NEAR_STOCKER", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "CREATE_DT", field: "CREATE_DT", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "MODIFY_DT", field: "MODIFY_DT", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "LASTMODIFY_DT", field: "LASTMODIFY_DT", headerHozAlign:"center", hozAlign: "center", width: 180 },
                { title: "UPDATED_DATE", field: "UPDATED_DATE", headerHozAlign:"center", hozAlign: "center", width: 180 },
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
  
  
      return { editData, priorityEditOpen, closeDrawer, toast, EquipListTabulator, initEquipList, refreshTableData, allEquipList, EquipListTableData, EquipListRef }
  
      },
      data() {
      return {
          allList: '',
      };
      },
      methods: {
      getAllEquipList() {
          
          const api = 'http://192.168.0.88:5001/GetUIData/GetEquipmentInformation';
          this.$http.get(api)
          .then((res) => {
  
              if (res.statusText == "OK") {
              // console.log('取得CARIER資料', res);
              // this.EquipListTableData.value = [{"UUID":"U2022090606240000182","CMD_ID":"C20220906062400362","CMD_TYPE":"TRANS","EQUIPID":"3PBG1-D","CMD_STATE":"Failed","CMD_CURRENT_STATE":"Failed","EquipID":"12-F02","EquipTYPE":"A12","SOURCE":"*","DEST":"3PBG1-D-LP03","PRIORITY":10.0,"REPLACE":3.0,"BACK":"*","INITIAL_DT":null,"WAITINGQUEUE_DT":null,"EXECUTEQUEUE_DT":null,"COMPLITE_DT":null,"LASTMODIFY_DT":"2022-09-06T07:11:37","ISLOCK":0.0}]
              this.EquipListTableData.value = res.data
              console.log('取得Equip資料CHANGE', this.EquipListTableData.value );
              }
              this.initEquipList()
          })
          .catch((error) => {
              console.log(error, '連線錯誤');
          });
      },
      },
      created () {
          this.getAllEquipList()
      },
  }
  
  </script>
  
  <template>
          <div class="p-4 bg-white rounded-lg md:p-8 dark:bg-gray-800" id="equipment" role="tabpanel" aria-labelledby="Equip-tab" forceRender>
              <h2 class="mb-5 text-2xl font-extrabold tracking-tight text-gray-900 dark:text-white">Current：  EQUIPMENT</h2>
              <div id="EquipListTabulator" ref="EquipListRef" class="text-sm text-left text-gray-500 dark:text-gray-400 pb-3"></div>
          </div>
  </template>