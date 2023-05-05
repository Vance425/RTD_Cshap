import axios from "axios";



export default function (){
  let responseData = null;
  const getData = async function(apiLink){
    try{
      responseData = await axios.get(apiLink);
      let data = responseData.data;
      console.log("MIXINDATA", data)
      return { data }
    } catch (error){
      console.log(error);
    }
  }
  return { getData };
}