<script>
    import { onBeforeMount, onMounted, reactive, ref, watch, getCurrentInstance, inject } from 'vue'; 
    import { storeToRefs } from 'pinia';
    import { useAuthStore } from '@/stores';
    import { TabulatorFull as Tabulator } from 'tabulator-tables';
    import { useToast } from "vue-toastification";
    
    export default {
        setup(props, cxt) {
            const currentInstance = getCurrentInstance()
            const { $http, $message, $route, $axios } = currentInstance.appContext.config.globalProperties
            const toast = useToast();
            const tableRef = ref(); 
            const tabulator = ref(); 
            const tableData = reactive([]);
            let eqpList = reactive()
            let carrierList = reactive()
            let selectStatus = reactive({
                eqp: '',
                carrier: ''
            })

            let modalSel = reactive()
            let editData = reactive({})
            let priorityEditOpen = ref(false)
            
            let optionFormatter = function(cell, formatterParams, onRendered) {
                return `<button class="block w-full dark:text-white font-medium text-sm px-5 py-5 text-center type="button"> ${ cell.getData().LOTID } </button>`;
            }
    
            
    
        const initTabulator = () => {
            tabulator.value = new Tabulator(tableRef.value, {
                placeholder:"No Data Available", 
                data:tableData.value, 
                autoResize:false,
                pagination: "remote",
                paginationSize: 10,
                rowHeight: 55,
                paginationSizeSelector: [10,25,50,100],
                paginationCounter: "rows",
                layout:"fitColumns",
                responsiveLayout: "collapse",
                reactiveData:true, 
                dataLoader: true,
                selectable: 1,
                height:690,
                columns:[
                    {
                        title:"LOT ID", 
                        field:"LOTID",
                        width:150, 
                        resizable: false,
                        headerHozAlign:"center",
                        hozAlign: "center",
                        formatter: optionFormatter,
                        cellClick:globalCellClick
                    },
                    {
                        title:"CARRIER ASSOCIATE", 
                        field:"CARRIER_ASSO", 
                        headerHozAlign:"center",
                        hozAlign: "center",
                        formatter(cell) {
                            return `<span class="text-sm font-medium h-full pt-4 block ${ cell.getData().CARRIER_ASSO == 'Y' ? "bg-teal-700 text-slate-200" : "bg-rose-500 text-slate-200"
                            }"> ${ cell.getData().CARRIER_ASSO == 'Y' ? "Y" : "N" } </span>`;
                        },
                    },
                    {
                        title:"DEVICE ASSOCIATE", 
                        field:"EQUIP_ASSO", 
                        headerHozAlign:"center",
                        hozAlign: "center",
                        formatter(cell) {
                            return `<span class="text-sm font-medium h-full pt-4 block ${ cell.getData().EQUIP_ASSO == 'Y' ? "bg-teal-700 text-slate-200" : "bg-rose-500 text-slate-200"
                            }"> ${ cell.getData().EQUIP_ASSO == 'Y' ? "Y" : "N" } </span>`;
                        },
                    },
                    {
                        title:"STATUS", 
                        field:"STATE", 
                        headerHozAlign:"center",
                        hozAlign: "center",
                        formatter(cell) {
                            return `<span class="text-sm font-medium block mt-4 mx-10 px-2.5 py-0.5 rounded-xl ${ cell.getData().STATE == 'HOLD' ? "bg-yellow-200 text-yellow-800 dark:bg-yellow-200 dark:text-yellow-900" : cell.getData().STATE == 'INIT' ? "bg-green-200 text-green-800 dark:bg-green-200 dark:text-green-900": "bg-red-200 text-red-800 dark:bg-red-200 dark:text-red-900" }"> ${ cell.getData().STATE } </span>`;
                        },
                    },
                    {title:"CREATE DATE", field:"CREATE_DT", headerHozAlign:"center", hozAlign: "center",},
                    {title:"LAST MODIFIED DATE", field:"LASTMODIFY_DT", headerHozAlign:"center", hozAlign: "center",}
                ],
    
            })
        }

        const getEqplist = function(id) {
            console.log("EQPLIST ID", id)
            $http.post('http://192.168.0.88:5001/GetUIData/GetEquipListByLotId', id)
            .then((res) => {
                // console.log("GET", res)
                if (res.statusText == "OK") {
                    // console.log("EQPLIST", res.data)
                    eqpList = {
                        eqpList: res.data
                    }
                    Object.assign(editData, eqpList)
                    console.log("FINAL EQPLIST", eqpList)
                }
            })
        }

        const getCarrierlist = function(id) {
            // console.log("CarrierLIST ID", id)
            $http.post('http://192.168.0.88:5001/GetUIData/GetCarrierByLotId', id)
            .then((res) => {
                // console.log("GET", res)
                if (res.statusText == "OK") {
                    carrierList = {
                        carrierList: res.data
                    }
                    Object.assign(editData, carrierList)
                    console.log("FINAL CarrierLIST", editData)
                }
            })
        }

    
        const globalCellClick = function(e, cell){
            let row = cell.getData();
            Object.assign(editData, row)
            priorityEditOpen.value = false
            console.log("SELECTEDIT", JSON.stringify(editData))
            let id = { lotId: editData.LOTID }
            getEqplist(id)
            getCarrierlist(id)
            modalSel.show()
        }
    
        function closeDrawer(){
            modalSel.hide()
        }
        
        function refreshTableData() {
            console.log("DATA REFRESH")
            this.getLotlist()
            console.log("DATA REFRESH 2")
        }


    
        onMounted( async () => {
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

        watch(() => selectStatus.eqp, () => selectStatus.carrier)
    
    
        return { tabulator, tableRef, editData, priorityEditOpen, closeDrawer, toast, initTabulator,  refreshTableData, tableData, eqpList, carrierList, selectStatus }
        // onMounted(() => {
    
        //     axios.get("https://ghibliapi.herokuapp.com/films")
        //         .then(res => {
        //             let resData = res.data
        //             console.log("DATA", resData)
        //             initTabulator(resData);
        //         })
        //         .catch(error => {
        //             console.log("ERROR", error)
        //         })
        //     return {resData}
        // })
        },
        data() {
            return {
                initClass: 'border-green-800 bg-green-200 text-green-800 dark:border-green-800 dark:bg-green-200 dark:text-green-900',
                readyClass: 'border-red-800 bg-red-200 text-red-800 dark:border-red-800 dark:bg-red-200 dark:text-red-900',
                holdClass: 'border-yellow-800 bg-yellow-200 text-yellow-800 dark:border-yellow-800 dark:bg-yellow-200 dark:text-yellow-900',
                complete: '',
            }
        },
        methods: {
            getLotlist() {
                
                this.$http.get('http://192.168.0.88:5001/GetUIData/GetLotInfoData')
                .then((res) => {
                    console.log("GET", res)
                    if (res.statusText == "OK") {

                        let changeData = res.data.map( e => {
                            return {
                                LOTID: e.LOTID,
                                PRIORITY:e.PRIORITY,
                                CARRIER_ASSO: e.CARRIER_ASSO,
                                EQUIP_ASSO: e.EQUIP_ASSO,
                                EQUIPLIST: e.EQUIPLIST,
                                STATE: e.STATE,
                                CREATE_DT: e.CREATE_DT,
                                LASTMODIFY_DT: e.LASTMODIFY_DT,
                            }
                        });
                        this.tableData.value = changeData
                        console.log("TESTAPI", this.tableData.value)
                        this.initTabulator()
                    }
                })
            },
            holdLot(){
                const url = 'http://192.168.0.88:5001/HoldLot'
                const postLot = {
                    "lotID": this.editData.LOTID
                }
                console.log('HOLD', postLot)
    
                this.$axios.post(url,  postLot )
                    .then( (response) => {
                        console.log(response)
                        this.toast.success("Hold Lot Success", {
                            position: "bottom-right",
                            timeout: 2000,
                        });
                        this.editData.STATE = 'HOLD'
                        this.refreshTableData()
                    })
                    .catch( (error) => {
                        console.log(error)   
                        this.toast.error(`Hold Lot Failed, ${error.message}`, {
                            position: "bottom-right",
                            timeout: 2000
                        });
                    })
            },
            releaseLot(){
                const url = 'http://192.168.0.88:5001/ReleaseLot'
                const postLot = {
                    "lotID": this.editData.LOTID
                }
                console.log('RELEASE', postLot)
    
                this.$axios.post(url, postLot )
                    .then( (response) => {
                        console.log(response)
                        this.toast.success("Release Lot Success", {
                            position: "bottom-right",
                            timeout: 2000
                        });
                        this.editData.STATE = 'INIT'
                        this.refreshTableData()
                    })
                    .catch( (error) => {
                        console.log(error)   
                        this.toast.error(`Release Lot Failed, ${error.message}`, {
                            position: "bottom-right",
                            timeout: 2000
                        });
                    })
            },
            dispatchLot(){
                const url = 'http://192.168.0.88:5001/MoveCarrier'
                console.log('DISPATCHSTATUS', this.selectStatus)
                const postLot = {
                    source: '',
                    dest: this.selectStatus.eqp,
                    lotID: this.editData.LOTID,
                    quantity: this.editData.PRIORITY,
                    carrierType: '',
                    commandType: '',
                    carrierID: this.selectStatus.carrier
                }
                console.log('DISPATCH', postLot)
    
                this.$axios.post(url,  postLot )
                    .then( (response) => {
                        console.log(response)
                        this.toast.success("Move Lot Success", {
                            position: "bottom-right",
                            timeout: 2000,
                        });
                        this.refreshTableData()
                    })
                    .catch( (error) => {
                        console.log(error)   
                        this.toast.error(`Move Lot Failed, ${error.message}`, {
                            position: "bottom-right",
                            timeout: 2000
                        });
                    })
            },
        },
        created () {
            this.getLotlist()
        },
    }
    
    </script>
    
    <template>
            <div class="p-2 bg-white rounded-lg md:p-8 dark:bg-gray-800" id="lot" role="tabpanel" aria-labelledby="lot-tab">
                <div class="flex flex-row items-center mb-2">
                    <h2 class="flex-col w-2/3 mb-5 text-2xl font-extrabold tracking-tight text-gray-900 dark:text-white">Current：  LOT</h2>
                    <form class="flex-col w-1/3 -mt-3">
                        <label for="search" class="mb-2 text-sm font-medium text-gray-900 sr-only dark:text-gray-300"></label>
                        <div class="relative">
                            <div class="flex absolute inset-y-0 left-0 items-center pl-3 pointer-events-none">
                                <svg aria-hidden="true" class="w-5 h-5 text-gray-500 dark:text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path></svg>
                            </div>
                            <input type="search" id="search" class="block p-4 pl-10 w-full text-sm text-gray-900 bg-gray-50 rounded-lg border border-gray-300 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" placeholder="Search" required>
                            <button type="submit" class="text-white absolute right-2.5 bottom-2.5 bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800">Search</button>
                        </div>
                    </form>
                    <button type="button" class="text-white bg-blue-700 hover:bg-blue-800 focus:ring-4 focus:outline-none focus:ring-blue-300 font-medium rounded-lg text-sm px-4 py-2 ml-4 mb-4 dark:bg-blue-600 dark:hover:bg-blue-700 dark:focus:ring-blue-800" @click="getLotlist">Refresh</button>
                </div>
                <div id="tabulator" ref="tableRef" class="text-sm text-left text-gray-500 dark:text-gray-400 pb-3"></div>
                <!-- drawer component -->
            <div id="drawer-dataform" class="fixed z-40 w-1/3 h-screen p-4 overflow-y-auto bg-white w-full dark:bg-gray-800" tabindex="-1" aria-labelledby="drawer-dataform-label">
            <h5 id="drawer-label" class="inline-flex items-center mt-2 mb-6 text-2xl font-semibold text-gray-500 uppercase dark:text-gray-400">LOT Control</h5>
            <button type="button" class="text-gray-400 bg-transparent hover:bg-gray-200 hover:text-gray-900 rounded-lg text-sm p-1.5 absolute top-2.5 right-2.5 inline-flex items-center dark:hover:bg-gray-600 dark:hover:text-white" @click="closeDrawer">
                    <svg aria-hidden="true" class="w-10 h-10" fill="currentColor" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg"><path fill-rule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clip-rule="evenodd"></path></svg>
                    <span class="sr-only">Close menu</span>
                </button>
                        <div class="p-4 mb-4 w-full bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
                            <div class="flex justify-between items-center mb-4">
                                <h5 class="text-xl font-bold leading-none text-gray-900 dark:text-gray-400">Status</h5>
                            </div>
                            <div class="flow-root">
                                <div class="p-4 mt-4 w-full bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                    <div class="flex flex-row">
                                        <div class="mb-6 flex-col w-1/2 mr-5">
                                            <label for="lotid" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">LOT ID</label>
                                            <input type="text" id="lotid" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
                                            v-model="editData.LOTID" :placeholder="editData.LOTID" :readonly="true">
                                        </div>
                                        <div class="mb-6 flex-col w-1/2">
                                            <label for="state01" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">Status</label>
                                            <input type="text" id="state01" class="border text-center border-gray-300 text-sm rounded-lg  block w-full p-2.5" :class="[editData.STATE == 'INIT' ? initClass : '', editData.STATE == 'HOLD' ? holdClass : '', editData.STATE == 'READY' ? readyClass : '']" v-model="editData.STATE" :placeholder="editData.STATE" :readonly="true">
                                        </div>
                                    </div>
                                    <div class="flex flex-row">
                                        <div class="mb-6 flex-col w-1/2 mr-3">
                                                <div class="relative w-full">
                                                    <label for="priority w-full" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-300">PRIORITY</label>
                                                    <input type="number" min="0" max="99" id="priority" class="w-full bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500" v-model="editData.PRIORITY" :placeholder="editData.PRIORITY"  v-show="!priorityEditOpen" @click="priorityEditOpen = !priorityEditOpen" required>
                                                </div>
                                                <div v-show="priorityEditOpen" class="flex justify-between items-center w-full">
                                                    <div class="relative w-full">
                                                        <input type="number" min="0" max="99" id="priority" class="block p-2.5 w-full z-20 text-sm text-gray-900 bg-gray-50 rounded-lg border-l-gray-100 border-l-2 border border-gray-300 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:border-blue-500" v-model="editData.PRIORITY" :placeholder="editData.PRIORITY" required>
                                                        <!-- <button type="submit" class="absolute top-0 right-10 p-2.5 text-sm font-medium text-white bg-red-500  border border-red-500 hover:bg-red-800 focus:ring-4 focus:outline-none focus:ring-red-300 dark:bg-red-500 dark:hover:bg-red-500 dark:focus:ring-red-800" @click="priorityEditOpen = false"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg></button> -->
                                                        <button type="submit" class="absolute top-0 right-0 p-2.5 text-sm font-medium text-white bg-teal-700 rounded-r-lg border border-teal-700 hover:bg-teal-800 focus:ring-4 focus:outline-none focus:ring-teal-300 dark:bg-teal-600 dark:hover:bg-teal-700 dark:focus:ring-teal-800" @click="priorityEditOpen = false"><svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg></button>
                                                    </div>
                                            </div>
                                        </div>
                                        <div v-show="priorityEditOpen" class="mb-6 flex-col w-1/2">
                                            <label for="minmax-range" class="block mb-4 text-sm font-medium text-gray-900 dark:text-gray-300">Min-Max Range</label>
                                            <input id="minmax-range" type="range" min="0" max="99" v-model="editData.PRIORITY" class="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer range-lg dark:bg-gray-700" >
                                        </div>
                                    </div>
                                    <div class="mb-6">
                                        <label for="countries" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-400">Equipment</label>
                                        <select v-model="selectStatus.eqp" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
                                            <option value="" selected>Select Equipment</option>
                                            <option
                                                v-for="(state, i) in editData.eqpList"
                                                :value="state"
                                                :key="i"
                                                >{{ state }}</option
                                            >
                                        </select>
                                    </div>
                                    <div class="mb-3">
                                        <label for="countries" class="block mb-2 text-sm font-medium text-gray-900 dark:text-gray-400">Carrier ID</label>
                                        <select v-model="selectStatus.carrier" class="bg-gray-50 border border-gray-300 text-gray-900 text-sm rounded-lg focus:ring-blue-500 focus:border-blue-500 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500">
                                            <option value="" selected>Select Carrier ID {{ carrierList }}</option>
                                            <option
                                                v-for="state in editData.carrierList"
                                                :value="state"
                                                >{{ state }}</option
                                            >
                                        </select>
                                    </div>
                                </div>
                                <!-- <hr class="my-3 border-slate-400 dark:border-slate-600">
                                <div class="p-4 w-full bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                    <div class="flow-root">
                                        
                                    </div>
                                </div> -->
                            </div>
                        </div>
                        <div class="p-4 mb-4 w-full bg-white rounded-lg border shadow-md dark:bg-gray-800 dark:border-gray-700">
                            <div class="flex justify-between items-center mb-4">
                                <h5 class="text-xl font-bold leading-none text-gray-900 dark:text-gray-400">ACTIONS</h5>
                            </div>
                            <div class="flow-root">
                                <div class="p-4 mt-4 w-full bg-gray-200 rounded-lg border shadow-md dark:bg-gray-900 dark:border-gray-700">
                                    <div class="flex flex-row">
                                        <div v-if="editData.STATE == 'INIT'" class="flex-col w-1/2 pr-5">
                                            <button id="hold" class="text-white bg-teal-700 hover:bg-teal-800 w-full focus:ring-4 focus:ring-teal-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-teal-600 dark:hover:bg-teal-700 focus:outline-none dark:focus:ring-teal-800 block" @click.prevent="holdLot">HOLD</button>
                                        </div>
                                        <div v-if="editData.STATE == 'READY'" class="flex-col w-1/2 pr-5">
                                            <button id="ready" class="text-white bg-teal-700 hover:bg-teal-800 w-full focus:ring-4 focus:ring-teal-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-teal-600 dark:hover:bg-teal-700 focus:outline-none dark:focus:ring-teal-800 block" @click.prevent="holdLot">HOLD</button>
                                        </div>
                                        <div v-if="editData.STATE == 'HOLD'" class="flex-col w-1/2 pr-5">
                                            <button id="release" class="text-white bg-yellow-700 hover:bg-yellow-800 w-full focus:ring-4 focus:ring-yellow-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-yellow-600 dark:hover:bg-yellow-700 focus:outline-none dark:focus:ring-yellow-800 block" @click.prevent="releaseLot">RELEASE</button>
                                        </div>
                                        <div class="flex-col w-1/2">
                                            <button type="submit" class="text-white bg-teal-700 hover:bg-teal-800 w-full focus:ring-4 focus:ring-teal-300 font-medium rounded-lg text-sm px-5 py-2.5 mr-2 dark:bg-teal-600 dark:hover:bg-teal-700 focus:outline-none dark:focus:ring-teal-800 block" @click="dispatchLot">DISPATCH</button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
            </div>
            </div>
    </template>